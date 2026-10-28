namespace NotificationEngine.Core.Responses;

public class ErrorDataResponse<T> : DataResponse<T>
{
    public string ExceptionMessage { get; }

    public ErrorDataResponse(T data, string exceptionMessage, string message) : base(data, false, message)
    {
        ExceptionMessage = exceptionMessage;
    }

    public ErrorDataResponse(T data, string message) : base(data, false, message)
    {
        ExceptionMessage = "General Exception";
    }

    public ErrorDataResponse(T data) : base(data, false, "Failed with errors")
    {
        ExceptionMessage = "General Exception";
    }

    public ErrorDataResponse(string message) : base(default, false, message) {
        ExceptionMessage = "General Exception";
    }
}
