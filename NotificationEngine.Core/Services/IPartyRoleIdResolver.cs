namespace NotificationEngine.Core.Services;

public interface IPartyRoleIdResolver
{
    Task<int> ResolvePartyRoleIdAsync(int applicationUserId, CancellationToken ct = default);
}
