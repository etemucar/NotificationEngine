using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Queries;

public sealed record GetMessageQuery(Guid Id) : IRequest<MessageResponse>;

/// <summary>Statuses null → filtre yok; boş liste → hiçbir kayıt eşleşmez.</summary>
public sealed record GetMessageListQuery(
    int Offset,
    int Limit,
    IReadOnlyList<MessageStatus>? Statuses) : IRequest<MessageListResponse>;