namespace NotificationEngine.Core.Responses;

public class ErrorResponse : Response
{
    public string ExceptionMessage { get; }
    public IDictionary<string, string[]>? FieldErrors { get; }

    public ErrorResponse(string message, IDictionary<string, string[]>? fieldErrors = null) : base(false, message)
    {
        ExceptionMessage = "General Exception";
        FieldErrors = fieldErrors;
    }

    public ErrorResponse(string exceptionMessage, string message, IDictionary<string, string[]>? fieldErrors = null) : base(false, message)
    {
        ExceptionMessage = exceptionMessage;
        FieldErrors = fieldErrors;
    }

    public ErrorResponse() : base(false, "Failed with errors")
    {
        ExceptionMessage = "General Exception";
    }
}
