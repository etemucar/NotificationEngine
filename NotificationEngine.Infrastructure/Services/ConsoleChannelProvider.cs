using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>
/// Geliştirme sağlayıcısı: gerçek gönderim yapmaz, log'a yazar. Her kanal için bir örnek kaydedilir.
/// DİKKAT: gövdeyi (OTP dahil) log'a yazar — Production'da asla seçilmemeli.
/// </summary>
public sealed class ConsoleChannelProvider : IChannelProvider
{
    private readonly ILogger<ConsoleChannelProvider> _logger;

    public ConsoleChannelProvider(Channel channel, ILogger<ConsoleChannelProvider> logger)
    {
        Channel = channel;
        _logger = logger;
    }

    public Channel Channel { get; }
    public string Name => NotificationConstants.ConsoleProviderName;

    public Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        _logger.LogWarning(
            "[CONSOLE-{Channel}] id={MessageId} to={Recipient} subject={Subject} body={Body}",
            message.Channel, message.MessageId, RecipientMasker.Mask(message.Recipient),
            message.Subject, message.Body);

        return Task.FromResult(SendResult.Ok($"console-{message.MessageId:N}"));
    }
}