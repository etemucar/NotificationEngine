using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Responses;

public sealed class MessageTemplateResponse
{
    public int Id { get; set; }
    public int? OrganizationId { get; set; }
    public string Code { get; set; } = null!;
    public Channel Channel { get; set; }
    public string Culture { get; set; } = null!;
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public GeneralStatus Status { get; set; }
}