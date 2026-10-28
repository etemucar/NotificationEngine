using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using NotificationEngine.Core.Responses;
using NotificationEngine.Core.Security;
using NotificationEngine.Infrastructure.Security;

namespace NotificationEngine.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }

    public static AuthenticationBuilder AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tokenOptions = configuration
            .GetSection("AppSettings:TokenOptions")
            .Get<TokenOptions>()
            ?? throw new InvalidOperationException("TokenOptions yapılandırması bulunamadı.");

        var key = Encoding.ASCII.GetBytes(tokenOptions.SecurityKey);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Development. Production'da true yapın.
                options.SaveToken            = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = tokenOptions.Issuer,
                    ValidAudience            = tokenOptions.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ClockSkew                = TimeSpan.Zero
                };

                // Token önce Authorization header'dan, yoksa HttpOnly "auth" cookie'sinden okunur.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                            return Task.CompletedTask;

                        var cookieToken = ctx.Request.Cookies["auth"];
                        if (!string.IsNullOrEmpty(cookieToken))
                            ctx.Token = cookieToken;

                        return Task.CompletedTask;
                    },

                    // Refresh token aynı anahtarla imzalı; erişim token'ı olarak kullanılamasın.
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.HasClaim("token_type", "refresh") == true)
                            ctx.Fail("Refresh token erişim için kullanılamaz.");
                        return Task.CompletedTask;
                    },

                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                new ErrorResponse("Yetkisiz erişim. Lütfen giriş yapın."), jsonOptions));
                    },

                    OnForbidden = ctx =>
                    {
                        ctx.Response.StatusCode  = StatusCodes.Status403Forbidden;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            JsonSerializer.Serialize(
                                new ErrorResponse("Bu işlem için yetkiniz yok."), jsonOptions));
                    }
                };
            });
    }
}