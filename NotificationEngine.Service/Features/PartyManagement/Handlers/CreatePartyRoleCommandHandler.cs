using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Data;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;

namespace NotificationEngine.Service.Features.Handlers;

public class CreatePartyRoleCommandHandler : IRequestHandler<CreatePartyRoleCommand, PartyRoleResponse>
{
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePartyRoleCommandHandler> _logger;

    public CreatePartyRoleCommandHandler(
        IRepository<PartyRole, int> partyRoleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreatePartyRoleCommandHandler> logger)
    {
        _partyRoleRepository = partyRoleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PartyRoleResponse> Handle(CreatePartyRoleCommand request, CancellationToken cancellationToken)
    {
        var partyRole = new PartyRole
        {
            PartyId = request.PartyId,
            PartyRoleTypeId = request.PartyRoleTypeId,
            ValidForStart = request.ValidForStart ?? DateTime.MinValue,
            ValidForEnd = request.ValidForEnd ?? DateTime.MaxValue,
        };

        await _partyRoleRepository.AddAsync(partyRole, cancellationToken);

        // partyRole.Id, log ve response'ta doğru görünsün diye SaveChanges
        // burada manuel tetikleniyor (bkz. CreateIndividualCommandHandler'daki
        // aynı gerekçe — TransactionalBehavior'ın SaveChanges'i next()
        // döndükten SONRA çağırdığı unutulmamalı).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "PartyRole oluşturuldu. PartyId: {PartyId}, PartyRoleTypeCd: {PartyRoleTypeCd}, PartyRoleId: {PartyRoleId}",
            partyRole.PartyId, partyRole.PartyRoleTypeId, partyRole.Id);

        return _mapper.Map<PartyRoleResponse>(partyRole);
    }
}
