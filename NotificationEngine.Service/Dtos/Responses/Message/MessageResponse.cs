using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Responses;

public sealed class MessageResponse
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public Channel Channel { get; set; }
    public MessagePriority Priority { get; set; }
    public string Recipient { get; set; } = default!;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? TemplateCode { get; set; }
    public MessageStatus DeliveryStatus { get; set; }
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CreateDate { get; set; }
}