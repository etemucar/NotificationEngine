using MediatR;
using Microsoft.Extensions.Logging;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, bool>
{
    private readonly IRepository<Organization, int> _organizationRepository;
    private readonly ILogger<DeleteOrganizationCommandHandler> _logger;

    public DeleteOrganizationCommandHandler(
        IRepository<Organization, int> organizationRepository,
        ILogger<DeleteOrganizationCommandHandler> logger)
    {
        _organizationRepository = organizationRepository;
        _logger                 = logger;
    }

    public async Task<bool> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByIdAsync(request.Id, cancellationToken);

        if (organization is null)
            return false;

        await _organizationRepository.RemoveAsync(organization, cancellationToken);

        _logger.LogInformation("Organization silindi. OrganizationId: {OrganizationId}", organization.Id);

        return true;
    }
}
