using AutoMapper;
using MediatR;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class GetMessageQueryHandler : IRequestHandler<GetMessageQuery, MessageResponse>
{
    private readonly IRepository<OutboxMessage, Guid> _messages;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetMessageQueryHandler(
        IRepository<OutboxMessage, Guid> messages, ICurrentUserService currentUser, IMapper mapper)
    {
        _messages = messages;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<MessageResponse> Handle(GetMessageQuery request, CancellationToken ct)
    {
        var organizationId = await _currentUser.GetOrganizationIdAsync(ct);

        // Başka kuruluşun mesajı da "bulunamadı" döner (varlığı sızdırılmaz).
        var message = await _messages.FindOneAsync(
            m => m.Id == request.Id && m.OrganizationId == organizationId, ct: ct)
            ?? throw new NotFoundException($"Mesaj bulunamadı: {request.Id}");

        return _mapper.Map<MessageResponse>(message);
    }
}