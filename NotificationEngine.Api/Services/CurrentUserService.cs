using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NotificationEngine.Api.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Common.References;

namespace NotificationEngine.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<ApplicationUser, int> _applicationUserRepository;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IRepository<ApplicationUser, int> applicationUserRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationUserRepository = applicationUserRepository;
    }

    public int? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Sid)?.Value;

            return !string.IsNullOrEmpty(claim) ? Convert.ToInt32(claim) : null;
        }
    }

    // JWT'ye PartyRoleId hiç konmuyor (TokenService.cs'e göre sadece
    // ApplicationUser.Id (ClaimTypes.Sid) taşınıyor) — bu yüzden her
    // ihtiyaç duyulduğunda ApplicationUser → DigitalIdentity → PartyRole
    // zincirinden bir DB lookup gerekiyor. Bunu her controller'da tekrar
    // yazmak yerine burada, tek yerde, merkezi olarak çözüyoruz.
    //
    // Performans notu: bu her çağrıda bir DB round-trip demek. İleride
    // PartyRoleId'yi login sırasında JWT claim'ine de eklerseniz (TokenService.
    // CreateAccessToken'a bir Claim daha eklemek yeterli), bu metodu
    // claim'den okuyacak şekilde sadeleştirebiliriz — DB'ye hiç gitmeden.
    public async Task<int> GetPartyRoleIdAsync(CancellationToken ct = default)
    {
        var userId = UserId ?? throw new UnauthorizedException("Kullanıcı doğrulanamadı.");

        var user = await _applicationUserRepository.FindOneAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.DigitalIdentity).ThenInclude(di => di.PartyRole),
            ct: ct);

        var partyRoleId = user?.DigitalIdentity?.PartyRole?.Id;

        return partyRoleId
            ?? throw new NotFoundException("Kullanıcının PartyRole kaydı bulunamadı.");
    }

    // Mesaj sahipliği kuruluş (Organization) bazında. İki çağıran türü:
    //  - X-Api-Key ile gelen servis (FinYo, Censitor): ApiKeyAuthenticationHandler claim'i koydu, DB'ye gidilmez.
    //  - Kuruluşun insan kullanıcısı (JWT): ApplicationUser → DigitalIdentity → PartyRole → Party.
    //    Party doğrudan Organization ise (kuruluş yöneticisi) o; değilse Party'nin açık (ValidTo boş)
    //    "OrganizationMember" RelatedParty kaydının karşı tarafındaki Organization. Bu bağlantı
    //    veritabanında kısmi unique index ile teklenmiştir, dolayısıyla en fazla bir sonuç çıkar
    //    ve header gerekmez (başkasının kuruluş kimliğini yazma riski yok).
    public async Task<int> GetOrganizationIdAsync(CancellationToken ct = default)
    {
        var claimValue = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ApiKeyAuthConstants.OrganizationIdClaim)?.Value;
        if (int.TryParse(claimValue, out var fromClaim) && fromClaim > 0)
            return fromClaim;

        var userId = UserId ?? throw new UnauthorizedException("Kullanıcı doğrulanamadı.");
        var organizationMemberTypeId = ReferenceData.RelatedPartyType.OrganizationMember;

        var organizationId = await _applicationUserRepository.FindOneSelectAsync(
            u => u.Id == userId,
            u => u.DigitalIdentity.PartyRole.Party.Organization != null
                ? (int?)u.DigitalIdentity.PartyRole.Party.Organization.Id
                : u.DigitalIdentity.PartyRole.Party.RelatedParties!
                    .Where(r => r.RelatedPartyTypeId == organizationMemberTypeId && r.ValidTo == null)
                    .Select(r => r.Related.Organization != null ? (int?)r.Related.Organization.Id : null)
                    .FirstOrDefault(),
            ct: ct);

        return organizationId
            ?? throw new NotFoundException("Kullanıcının bağlı olduğu organizasyon bulunamadı.");
    }

    public bool IsAdmin
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            return claim == ReferenceData.PartyRoleType.SiteAdminCd;
        }
    }    
}