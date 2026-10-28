using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class GetPartyRoleQueryHandler : IRequestHandler<GetPartyRoleQuery, PartyRoleResponse>
{
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPartyRoleQueryHandler> _logger;

    public GetPartyRoleQueryHandler(
        IRepository<PartyRole, int> partyRoleRepository,
        IMapper mapper,
        ILogger<GetPartyRoleQueryHandler> logger)
    {
        _partyRoleRepository = partyRoleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PartyRoleResponse> Handle(GetPartyRoleQuery request, CancellationToken cancellationToken)
    {
        var partyRole = await _partyRoleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (partyRole is null)
            throw new NotFoundException($"PartyRole bulunamadı: {request.Id}");

        return _mapper.Map<PartyRoleResponse>(partyRole);
    }
}
