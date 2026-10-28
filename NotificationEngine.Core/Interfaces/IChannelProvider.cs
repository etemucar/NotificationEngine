using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Interfaces;

public interface IChannelProvider
{
    Channel Channel { get; }
    string Name { get; }   // yapılandırmada seçim anahtarı: "Console", "Ses", "Fcm"...
    Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct);
}

/// <summary>Sağlayıcıya verilen son hal. OTP'de Body yalnızca bellekte yaşar.</summary>
public sealed record RenderedMessage(
    Guid MessageId,
    Channel Channel,
    string Recipient,
    string? Subject,
    string Body,
    string? FromEmail,
    string? FromName,
    MessagePriority Priority);

public sealed record SendResult(
    bool Success,
    string? ProviderMessageId = null,
    string? Error = null,
    SendFailureKind FailureKind = SendFailureKind.None)
{
    public static SendResult Ok(string? providerMessageId = null) => new(true, providerMessageId);
    public static SendResult Transient(string error) => new(false, null, error, SendFailureKind.Transient);
    public static SendResult Permanent(string error) => new(false, null, error, SendFailureKind.Permanent);
    public static SendResult InvalidRecipient(string error) => new(false, null, error, SendFailureKind.InvalidRecipient);
}