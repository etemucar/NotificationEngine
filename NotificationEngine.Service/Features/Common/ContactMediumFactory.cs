using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Dtos.Requests;

namespace NotificationEngine.Service.Common;

/// <summary>
/// TMF contactMedium isteğini ContactMedium kaydına çevirir. Desteklenmeyen tür ya da boş değer
/// sessizce atlanmaz, 400 döner.
/// </summary>
internal static class ContactMediumFactory
{
    public static ContactMedium Create(ContactMediumRequest request)
    {
        if (!Enum.TryParse<ContactMediumType>(request.MediumType, ignoreCase: true, out var mediumType))
            throw new InvalidInputException($"Geçersiz iletişim türü: '{request.MediumType}'.");

        var medium = new ContactMedium
        {
            MediumType = mediumType,
            IsPreferred = request.Preferred
        };

        switch (mediumType)
        {
            case ContactMediumType.EmailAddress:
                medium.Email = Required(request, "emailAddress");
                break;
            case ContactMediumType.PhoneNumber:
                medium.PhoneNumber = Required(request, "phoneNumber");
                break;
            default:
                throw new InvalidInputException($"Desteklenmeyen iletişim türü: '{request.MediumType}'.");
        }

        return medium;
    }

    private static string Required(ContactMediumRequest request, string key)
    {
        var value = request.Characteristic
            .FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidInputException($"'{request.MediumType}' için '{key}' gerekli.");

        return value.Trim();
    }
}