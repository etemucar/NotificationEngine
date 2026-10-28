using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class PatchPartyRoleCommandHandler : IRequestHandler<PatchPartyRoleCommand, PartyRoleResponse>
{
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatchPartyRoleCommandHandler> _logger;

    public PatchPartyRoleCommandHandler(
        IRepository<PartyRole, int> partyRoleRepository,
        IMapper mapper,
        ILogger<PatchPartyRoleCommandHandler> logger)
    {
        _partyRoleRepository = partyRoleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PartyRoleResponse> Handle(PatchPartyRoleCommand request, CancellationToken cancellationToken)
    {
        var partyRole = await _partyRoleRepository.GetByIdAsync(request.Id, cancellationToken);

        if (partyRole is null)
            throw new NotFoundException($"PartyRole bulunamadı: {request.Id}");

        if (request.PartyRoleTypeId is not null)
        {
            partyRole.PartyRoleTypeId = request.PartyRoleTypeId.Value;
        }

        partyRole.ValidForStart = request.ValidForStart ?? partyRole.ValidForStart;
        partyRole.ValidForEnd = request.ValidForEnd ?? partyRole.ValidForEnd;

        await _partyRoleRepository.UpdateAsync(partyRole, cancellationToken);

        _logger.LogInformation("PartyRole güncellendi. PartyRoleId: {PartyRoleId}", partyRole.Id);

        return _mapper.Map<PartyRoleResponse>(partyRole);
    }
}
