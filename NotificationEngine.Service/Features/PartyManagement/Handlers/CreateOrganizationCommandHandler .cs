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

// NOT: Bu dosya, tekrar yüklenmedi ama CreateIndividualCommandHandler ile
// BİREBİR AYNI bug'ı taşıyordu (organization.PartyId = party.Id, SaveChanges'ten
// önce okunuyordu). Tutarlılık için aynı düzeltme burada da uygulandı.
public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, OrganizationResponse>
{
    private readonly IRepository<Party, int> _partyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrganizationCommandHandler> _logger;

    public CreateOrganizationCommandHandler(
        IRepository<Party, int> partyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateOrganizationCommandHandler> logger)
    {
        _partyRepository = partyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrganizationResponse> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = new Organization
        {
            Name = request.Name,
            TaxOffice = request.TaxOffice,
            TaxNumber = request.TaxNumber,
            IdentityNumber = request.IdentityNumber,
            TradeName = request.TradeName,
            TradeRegisterNumber = request.TradeRegisterNumber,
            MersisNo = request.MersisNo,
            ValidForStart = request.ValidForStart ?? DateTime.MinValue,
            ValidForEnd = request.ValidForEnd ?? DateTime.MaxValue,
        };

        // FK artık navigation property ile kuruluyor (bkz. CreateIndividualCommandHandler'daki
        // aynı gerekçe) — elle .Id okuma yok.
        var party = new Party
        {
            Organization = organization
        };

        await _partyRepository.AddAsync(party, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Organization oluşturuldu. PartyId: {PartyId}, OrganizationId: {OrganizationId}",
            party.Id, organization.Id);

        return _mapper.Map<OrganizationResponse>(organization);
    }
}
