// Infrastructure/DependencyInjection.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Security;
using NotificationEngine.Infrastructure.Repositories;
using NotificationEngine.Infrastructure.Security;
using NotificationEngine.Infrastructure.Service;

namespace NotificationEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddMemoryCache();

        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // ── Persistence ──────────────────────────────────────────────────
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<NotificationEngineDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxStore, OutboxStore>();

        // ── Kanal sağlayıcıları ──────────────────────────────────────────
        // Hangisinin kullanılacağı Notification:Providers:{Email|Sms|Push} yapılandırmasından seçilir.
        // Ayar yoksa Console (sahte) sağlayıcı devreye girer. Seçilmeyen sağlayıcılar kayıtlı kalır ama
        // kullanılmaz; seçenekleri ilk gönderimde okunur (açılışta patlamaz).
        foreach (var channel in Enum.GetValues<Channel>())
        {
            var ch = channel;
            services.AddSingleton<IChannelProvider>(sp =>
                new ConsoleChannelProvider(ch, sp.GetRequiredService<ILogger<ConsoleChannelProvider>>()));
        }
        services.AddScoped<IChannelProviderResolver, ChannelProviderResolver>();

        // E-posta: Amazon SES. Seçmek için: Notification:Providers:Email = "Ses"
        services.AddSingleton<IChannelProvider>(sp => new SesEmailProvider(
            SesOptions.From(configuration),
            sp.GetRequiredService<ILogger<SesEmailProvider>>()));

        // E-posta: SMTP. Seçmek için: Notification:Providers:Email = "Smtp"
        services.AddSingleton<IChannelProvider>(sp => new SmtpEmailProvider(
            SmtpOptions.From(configuration),
            sp.GetRequiredService<ILogger<SmtpEmailProvider>>()));

        // SMS: Netgsm. Seçmek için: Notification:Providers:Sms = "Netgsm"
        services.AddHttpClient(NetgsmSmsProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IChannelProvider>(sp => new NetgsmSmsProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            NetgsmOptions.From(configuration),
            sp.GetRequiredService<ILogger<NetgsmSmsProvider>>()));

        // SMS: İleti Merkezi. Seçmek için: Notification:Providers:Sms = "IletiMerkezi"
        services.AddHttpClient(IletiMerkeziSmsProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IChannelProvider>(sp => new IletiMerkeziSmsProvider(
            sp.GetRequiredService<IHttpClientFactory>(),
            IletiMerkeziOptions.From(configuration),
            sp.GetRequiredService<ILogger<IletiMerkeziSmsProvider>>()));

        // Push: Firebase Cloud Messaging. Seçmek için: Notification:Providers:Push = "Fcm"
        services.AddSingleton<IChannelProvider>(sp => new FcmPushProvider(
            FcmProviderOptions.From(configuration),
            sp.GetRequiredService<ILogger<FcmPushProvider>>()));

        return services;
    }
}