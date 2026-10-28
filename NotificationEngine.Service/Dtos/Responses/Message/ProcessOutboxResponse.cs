namespace NotificationEngine.Service.Dtos.Responses;

public sealed record ProcessOutboxResponse(
    int Claimed,
    int Sent,
    int Retried,
    int Failed
);