using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Interfaces;

/// <summary>
/// Kanal → aktif adapter eşlemesi (Notification:Providers:{Email|Sms|Push} yapılandırmasından).
/// Implementasyon Infrastructure'da; Service sadece bu interface'i görür.
/// </summary>
public interface IChannelProviderResolver
{
    IChannelProvider Resolve(Channel channel);
}