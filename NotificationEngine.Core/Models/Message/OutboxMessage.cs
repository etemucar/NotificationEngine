using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;

public class OutboxMessage : ModelBase<Guid>
{
    public int OrganizationId { get; set; }   // mesajın sahibi kuruluş
    public string? TemplateCode { get; set; }   // content modunda null
    public Channel Channel { get; set; }
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>E-posta, telefon (E.164) veya FCM cihaz token'ı. Loglarda maskelenir.</summary>
    public string Recipient { get; set; } = default!;

    // Render edilmiş, DONMUŞ içerik. OTP mesajlarında ikisi de null kalır (saklanmaz).
    public string? Subject { get; set; }
    public string? Body { get; set; }

    /// <summary>(OrganizationId, IdempotencyKey) üzerinde benzersiz.</summary>
    public string IdempotencyKey { get; set; } = default!;

    // ModelBase.Status (GeneralStatus) ile çakışmaması için adı DeliveryStatus.
    public MessageStatus DeliveryStatus { get; set; } = MessageStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LockedUntil { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<DeliveryLog> DeliveryLogs { get; set; } = new List<DeliveryLog>();
}