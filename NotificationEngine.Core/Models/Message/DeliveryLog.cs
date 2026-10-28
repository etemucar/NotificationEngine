using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;

/// <summary>Append-only: deneme geçmişi ve sağlayıcı webhook olayları.</summary>
public class DeliveryLog : ModelBase<long>
{
    public Guid MessageId { get; set; }
    public DeliveryEventType Event { get; set; }
    public string? Detail { get; set; }                   // alıcı/OTP içeriği ASLA yazılmaz
    public DateTime OccurredAt { get; set; }

    public OutboxMessage? Message { get; set; }
}