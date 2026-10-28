// GetPartyRoleListQueryHandler.cs
using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class GetPartyRoleListQueryHandler : IRequestHandler<GetPartyRoleListQuery, IEnumerable<PartyRoleResponse>>
{
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPartyRoleListQueryHandler> _logger;

    public GetPartyRoleListQueryHandler(
        IRepository<PartyRole, int> partyRoleRepository,
        IMapper mapper,
        ILogger<GetPartyRoleListQueryHandler> logger)
    {
        _partyRoleRepository = partyRoleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<PartyRoleResponse>> Handle(GetPartyRoleListQuery request, CancellationToken cancellationToken)
    {
        var partyRoles = await _partyRoleRepository.FindAsync(
            predicate: _ => true,
            orderBy: q => q.OrderBy(x => x.Id),
            ct: cancellationToken);

        return partyRoles.Select(_mapper.Map<PartyRoleResponse>);
    }
}
