using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace NotificationEngine.Service.Common;

public class PartyRoleIdResolver : IPartyRoleIdResolver
{
    private readonly IRepository<ApplicationUser, int> _applicationUserRepository;

    public PartyRoleIdResolver(
        IRepository<ApplicationUser, int> applicationUserRepository)
    {
        _applicationUserRepository = applicationUserRepository;
    }

    public async Task<int> ResolvePartyRoleIdAsync(int applicationUserId, CancellationToken ct = default)
    {
        var user = await _applicationUserRepository.FindOneAsync(
            u => u.Id == applicationUserId,
            include: q => q.Include(u => u.DigitalIdentity).ThenInclude(di => di.PartyRole),
            ct: ct);

        var partyRoleId = user?.DigitalIdentity?.PartyRole?.Id;

        return partyRoleId
            ?? throw new NotFoundException("Kullanıcının PartyRole kaydı bulunamadı.");        
    }
}
