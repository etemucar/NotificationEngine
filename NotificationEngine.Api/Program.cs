using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NotificationEngine.Api.Extensions;
using NotificationEngine.Api.Middleware;
using NotificationEngine.Api.Security;
using NotificationEngine.Api.Services;
using NotificationEngine.Api.Workers;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Services;
using NotificationEngine.Infrastructure;
using NotificationEngine.Service;
using NotificationEngine.Service.Behaviors;
using NotificationEngine.Service.Mapper;
using NotificationEngine.Settings.Core;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// ── Servisler ────────────────────────────────────────────────────────────
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
services.AddOpenApi();
services.AddHttpContextAccessor();
services.AddSingleton(TimeProvider.System);

// Coolify healthcheck (liveness): uygulama istek kabul ediyorsa 200 döner.
services.AddHealthChecks();

// ── Katman kayıtları ─────────────────────────────────────────────────────
services.AddInfrastructure(builder.Configuration);
services.AddServiceLayer(builder.Configuration);

var serviceAssembly = typeof(MappingProfile).Assembly;   // NotificationEngine.Service

services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(serviceAssembly);
    // Pipeline sırası: ValidationBehavior → TransactionalBehavior → Handler
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionalBehavior<,>));
});
services.AddValidatorsFromAssembly(serviceAssembly);
services.AddAutoMapper(cfg => cfg.AddMaps(serviceAssembly, typeof(Program).Assembly));

services.AddScoped<ICurrentUserService, CurrentUserService>();

// Anında gönderim (OTP): numara başına sınır (sayaç IMemoryCache'te, bu yüzden singleton).
services.AddSingleton<ISendNowRateLimiter, SendNowRateLimiter>();

// ── Arka plan: outbox'taki bekleyen mesajları gönderir ───────────────────
services.AddHostedService<OutboxWorker>();

// ── Kimlik doğrulama ─────────────────────────────────────────────────────
// JWT (varsayılan şema): kuruluşun insan kullanıcıları. ApiKey: FinYo/Censitor (X-Api-Key),
// yalnızca [Authorize(AuthenticationSchemes = ...)] ile seçilen uçlarda çalışır.
services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
services.AddApplicationServices();   // ITokenService
services.AddJwtAuthentication(builder.Configuration)
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthConstants.SchemeName, _ => { });

services.AddAuthorization(options =>
{
    // register-organization gibi yönetici uçları. CurrentUserService.IsAdmin ile aynı claim.
    options.AddPolicy("SiteAdmin", policy =>
        policy.RequireClaim(ClaimTypes.Role, ReferenceData.PartyRoleType.SiteAdminCd));
});

// ── Hız sınırı: kuruluş başına dakikalık genel sınır ("send-now" politikası) ──
// Numara başına sınır ayrıca ISendNowRateLimiter ile controller içinde uygulanır.
var orgPerMinute = builder.Configuration.GetValue("Notification:SendNow:OrganizationPerMinute", 60);
services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("send-now", httpContext =>
    {
        // ApiKeyAuthenticationHandler'ın eklediği claim; TEYİT: claim adı projedeki handler ile aynı olmalı.
        var org = httpContext.User.FindFirst("organization_id")?.Value ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(org, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = orgPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

// ── Deploy-time migration ────────────────────────────────────────────────
// Database:MigrateOnStartup=true (ortam değişkeni: Database__MigrateOnStartup) ise bekleyen migration'lar,
// uygulama istek almaya ve arka plan işleri (OutboxWorker) başlamadan önce uygulanır. Migration patlarsa
// süreç düşer, sağlık kontrolü geçmez ve eski konteyner ayakta kalır. Birden çok örnek aynı anda açılırsa
// EF'in migration kilidi sayesinde biri uygular, diğerleri bekler.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationEngineDbContext>();

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        logger.LogInformation("Bekleyen migration sayısı: {Count} ({Names})",
            pending.Count, string.Join(", ", pending));

        await db.Database.MigrateAsync();
        logger.LogInformation("Migration tamamlandı.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Migration başarısız oldu, uygulama başlatılmıyor.");
        throw;
    }
}

// ── HTTP pipeline ────────────────────────────────────────────────────────
// Reverse proxy (Coolify) arkasında doğru şema ve istemci IP'si için en başta.
// DİKKAT: bilinen proxy listesi boşaltıldığı için her kaynaktan gelen X-Forwarded-* kabul edilir;
// uygulama doğrudan internete açıksa istemci IP'si taklit edilebilir (hız sınırları IP'ye bakmıyor).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// İş istisnaları (InvalidInput/NotFound/Conflict/Validation) HTTP durum koduna burada çevrilir.
app.UseMiddleware<ExceptionMiddleware>();

// Dokümantasyon yalnızca geliştirme ortamında açık (yönetici uçları var).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Scalar:Enabled", false))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("NotificationEngine API Reference")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();   // UseAuthorization'dan SONRA: ApiKey kimliği o noktada User'a oturmuş olur

app.MapControllers();

// Coolify healthcheck yolu: GET /health -> 200 "Healthy"
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();