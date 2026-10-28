namespace NotificationEngine.Service.Dtos.Responses;

public sealed record MessageTemplateListResponse(IReadOnlyList<MessageTemplateResponse> Items, int TotalCount);