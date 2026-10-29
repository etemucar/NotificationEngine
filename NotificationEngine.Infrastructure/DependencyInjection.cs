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

        // ── Sağlayıcı ayarları ────────────────────────────────────────────
        // Her sınıf kendi From(configuration) metodu ile ilgili bölümü okur.
        // From metotları varsayılan değerleri ve mevcut anahtar eşlemelerini korur.
        // Singleton ayarlar ilk çözümlemede okunur; değişiklikler için uygulama yeniden başlatılır.
        services.AddSingleton<SesOptions>(_ => SesOptions.From(configuration));
        services.AddSingleton<SmtpOptions>(_ => SmtpOptions.From(configuration));
        services.AddSingleton<NetgsmOptions>(_ => NetgsmOptions.From(configuration));
        services.AddSingleton<IletiMerkeziOptions>(_ => IletiMerkeziOptions.From(configuration));
        services.AddSingleton<FcmProviderOptions>(_ => FcmProviderOptions.From(configuration));

        // ── Kanal sağlayıcıları ──────────────────────────────────────────
        // Hangisinin kullanılacağı Notification:Providers:{Email|Sms|Push} yapılandırmasından seçilir.
        // Ayar yoksa Console (sahte) sağlayıcı devreye girer.
        foreach (var channel in Enum.GetValues<Channel>())
        {
            var ch = channel;
            services.AddSingleton<IChannelProvider>(sp =>
                new ConsoleChannelProvider(ch, sp.GetRequiredService<ILogger<ConsoleChannelProvider>>()));
        }
        services.AddScoped<IChannelProviderResolver, ChannelProviderResolver>();

        // E-posta: Notification:Providers:Email = "Ses" veya "Smtp".
        // Kuruculardaki options ve logger bağımlılıkları DI tarafından sağlanır.
        services.AddSingleton<IChannelProvider, SesEmailProvider>();
        services.AddSingleton<IChannelProvider, SmtpEmailProvider>();

        // SMS: Notification:Providers:Sms = "Netgsm" veya "IletiMerkezi".
        services.AddHttpClient(NetgsmSmsProvider.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IChannelProvider, NetgsmSmsProvider>();

        services.AddHttpClient(IletiMerkeziSmsProvider.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IChannelProvider, IletiMerkeziSmsProvider>();

        // Push: Notification:Providers:Push = "Fcm".
        services.AddSingleton<IChannelProvider, FcmPushProvider>();

        return services;
    }
}
