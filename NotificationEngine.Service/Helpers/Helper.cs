using NotificationEngine.Core.Model;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Helpers;

public static class Helper
{
    public static (int userId, string userName, string userIdentifier) ResolveUserInfo(ApplicationUser user)
    {
        var individual  = user.DigitalIdentity.PartyRole?.Party?.Individual;

        var userName = individual != null
            ? $"{individual.GivenName} {individual.FamilyName}".Trim()
            : user.DigitalIdentity.Nickname ?? string.Empty;

        var contactMedium = user.DigitalIdentity.Credentials
            .SelectMany(c => c.ContactMedia)
            .FirstOrDefault();

        var userIdentifier = contactMedium?.Email
            ?? contactMedium?.PhoneNumber
            ?? string.Empty;

        return (user.Id, userName, userIdentifier);
    }

}
