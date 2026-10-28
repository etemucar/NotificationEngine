using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Responses;

public sealed record SendNowResponse(bool Success, SendFailureKind FailureKind, DateTime SentAt);