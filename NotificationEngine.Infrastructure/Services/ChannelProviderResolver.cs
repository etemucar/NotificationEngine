using Microsoft.Extensions.Configuration;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>
/// Notification:Providers:{Email|Sms|Push} değerindeki ada göre adapter seçer.
/// Yapılandırma yoksa "Console" varsayılır.
/// </summary>
public sealed class ChannelProviderResolver : IChannelProviderResolver
{
    private readonly IReadOnlyList<IChannelProvider> _providers;
    private readonly IConfiguration _configuration;

    public ChannelProviderResolver(IEnumerable<IChannelProvider> providers, IConfiguration configuration)
    {
        _providers = providers.ToList();
        _configuration = configuration;
    }

    public IChannelProvider Resolve(Channel channel)
    {
        var name = _configuration[$"{NotificationConstants.ProviderConfigPrefix}:{channel}"]
                   ?? NotificationConstants.ConsoleProviderName;

        var provider = _providers.FirstOrDefault(p =>
            p.Channel == channel && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new InvalidOperationException(
            $"'{channel}' kanalı için '{name}' adlı sağlayıcı kayıtlı değil " +
            $"({NotificationConstants.ProviderConfigPrefix}:{channel}).");
    }
}