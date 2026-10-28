using NotificationEngine.Core.Enums;

namespace NotificationEngine.Service.Dtos.Responses;

public class RegisterOrganizationResult
{
    public int PartyId { get; set; }
    public int OrganizationId { get; set; }
    public int PartyRoleId { get; set; }
    public Guid DigitalIdentityId { get; set; }

    /// <summary>ApplicationUser oluşturulmadıysa (ExternalService) null.</summary>
    public int? ApplicationUserId { get; set; }

    public List<IssuedCredentialInfo> IssuedCredentials { get; set; } = new();
}

public class IssuedCredentialInfo
{
    public Guid CredentialId { get; set; }
    public CredentialType CredentialType { get; set; }

    /// <summary>
    /// Sunucu tarafından otomatik üretilmiş ham değer (örn. caller kendi
    /// API key'ini vermediyse). SADECE bu response'ta görünür, bir daha
    /// geri okunamaz. Caller kendi değerini verdiyse (örn. password) null.
    /// </summary>
    public string? GeneratedRawValue { get; set; }
}