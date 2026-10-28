using AutoMapper;
using MediatR;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class GetMessageListQueryHandler : IRequestHandler<GetMessageListQuery, MessageListResponse>
{
    private readonly IRepository<OutboxMessage, Guid> _messages;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetMessageListQueryHandler(
        IRepository<OutboxMessage, Guid> messages, ICurrentUserService currentUser, IMapper mapper)
    {
        _messages = messages;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<MessageListResponse> Handle(GetMessageListQuery request, CancellationToken ct)
    {
        var organizationId = await _currentUser.GetOrganizationIdAsync(ct);
        var statuses = request.Statuses?.ToList();

        var items = await _messages.FindOffsetAsync(
            m => m.OrganizationId == organizationId && (statuses == null || statuses.Contains(m.DeliveryStatus)),
            request.Offset, request.Limit,
            orderBy: q => q.OrderByDescending(m => m.CreateDate),
            ct: ct);

        var total = await _messages.CountAsync(
            m => m.OrganizationId == organizationId && (statuses == null || statuses.Contains(m.DeliveryStatus)), ct);

        return new MessageListResponse
        {
            Items = _mapper.Map<List<MessageResponse>>(items),
            TotalCount = total
        };
    }
}