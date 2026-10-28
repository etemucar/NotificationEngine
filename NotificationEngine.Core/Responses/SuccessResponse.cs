namespace NotificationEngine.Core.Responses;

public class SuccessResponse : Response
{
    public SuccessResponse(string message) : base(true, message)
    {
    }

    public SuccessResponse() : base(true, "Successfully finished")
    {
    }
}
