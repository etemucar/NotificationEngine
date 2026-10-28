namespace NotificationEngine.Core.Responses;

public class SuccessDataResponse<T> : DataResponse<T>
{
    public SuccessDataResponse(T data, string message) : base(data, true, message)
    {
    }

    public SuccessDataResponse(T data) : base(data, true, "Successfully finished")
    {
    }
}
