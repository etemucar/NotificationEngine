namespace NotificationEngine.Core.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAdmin { get; }
    Task<int> GetPartyRoleIdAsync(CancellationToken ct);

    /// <summary>
    /// Çağıranın bağlı olduğu kuruluşun (Organization) Id'si. X-Api-Key ile gelen servis için claim'den,
    /// kuruluşun insan kullanıcısı için Party / OrganizationMember bağlantısından çözülür.
    /// </summary>
    Task<int> GetOrganizationIdAsync(CancellationToken ct);
}