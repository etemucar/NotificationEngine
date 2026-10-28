using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, OrganizationResponse>
{
    private readonly IRepository<Organization, int> _organizationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrganizationQueryHandler> _logger;

    public GetOrganizationQueryHandler(
        IRepository<Organization, int> organizationRepository,
        IMapper mapper,
        ILogger<GetOrganizationQueryHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrganizationResponse> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (organization is null)
            throw new NotFoundException($"Organization bulunamadı: {request.Id}");

        return _mapper.Map<OrganizationResponse>(organization);
    }
}
