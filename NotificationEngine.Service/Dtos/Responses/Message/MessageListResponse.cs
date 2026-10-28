namespace NotificationEngine.Service.Dtos.Responses;

public sealed class MessageListResponse
{
    public IReadOnlyList<MessageResponse> Items { get; set; } = Array.Empty<MessageResponse>();
    public int TotalCount { get; set; }
}