using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class PatchOrganizationCommandHandler : IRequestHandler<PatchOrganizationCommand, OrganizationResponse>
{
    private readonly IRepository<Organization, int> _organizationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatchOrganizationCommandHandler> _logger;

    public PatchOrganizationCommandHandler(
        IRepository<Organization, int> organizationRepository,
        IMapper mapper,
        ILogger<PatchOrganizationCommandHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrganizationResponse> Handle(PatchOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (organization is null)
            throw new NotFoundException($"Organization bulunamadı: {request.Id}");

        // Sadece gelen alanları güncelle (merge-patch)
        if (request.Name is not null) organization.Name = request.Name;
        if (request.TaxOffice is not null) organization.TaxOffice = request.TaxOffice;
        if (request.TaxNumber is not null) organization.TaxNumber = request.TaxNumber.Value;
        if (request.IdentityNumber is not null) organization.IdentityNumber = request.IdentityNumber.Value;
        if (request.TradeName is not null) organization.TradeName = request.TradeName;
        if (request.TradeRegisterNumber is not null) organization.TradeRegisterNumber = request.TradeRegisterNumber.Value;
        if (request.MersisNo is not null) organization.MersisNo = request.MersisNo.Value;

        organization.ValidForStart = request.ValidForStart ?? organization.ValidForStart;
        organization.ValidForEnd = request.ValidForEnd ?? organization.ValidForEnd;

        await _organizationRepository.UpdateAsync(organization, cancellationToken);

        _logger.LogInformation("Organization güncellendi. OrganizationId: {OrganizationId}", organization.Id);

        return _mapper.Map<OrganizationResponse>(organization);
    }
}
