namespace NotificationEngine.Core.Responses;

public class Response : IResponse
{
    public Response(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }
}
