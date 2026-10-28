using System.Security.Claims;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Core.Security;

public interface ITokenService
{
    AccessToken CreateAccessToken(ApplicationUser user);
    AccessToken CreateAccessToken(string userName, int userId, string userCredential, string roleCd);
    RefreshToken CreateRefreshToken(int userId);
    ClaimsPrincipal? ValidateToken(string token);
}
