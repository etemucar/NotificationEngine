namespace NotificationEngine.Core.Responses;

public class DataResponse<T> : Response, IDataResponse<T>
{
    public DataResponse(T? data, bool success, string message) : base(success, message)
    {
        Data = data;
    }

    public T? Data { get; }
}
