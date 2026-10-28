using System.ComponentModel.DataAnnotations;
using NotificationEngine.Api.Models.TMFOpenApi5;

namespace NotificationEngine.Api.Models;

/// <summary>TMF dışı - Auth/Admin ile aynı konvansiyon. Credentials için TMF'in CredentialModel'ı reuse ediliyor.</summary>
public class RegisterOrganizationModel
{
    [Required(ErrorMessage = "Name zorunludur.")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public string? TaxOffice { get; set; }
    public long TaxNumber { get; set; }
    public long IdentityNumber { get; set; }
    public string? TradeName { get; set; }
    public long TradeRegisterNumber { get; set; }
    public long MersisNo { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd { get; set; }

    [Required(ErrorMessage = "PartyRoleTypeId zorunludur.")]
    public int PartyRoleTypeId { get; set; }

    public string? Nickname { get; set; }

    /// <summary>ApplicationUser oluşacaksa (PartyRoleTypeId != ExternalService) zorunlu - handler doğrular.</summary>
    public int? LanguageId { get; set; }

    public List<CredentialModel> Credentials { get; set; } = new();
}

public class RegisterOrganizationResponse
{
    public bool Success { get; set; } = true;
    public int PartyId { get; set; }
    public int OrganizationId { get; set; }
    public int PartyRoleId { get; set; }
    public Guid DigitalIdentityId { get; set; }

    /// <summary>ApplicationUser oluşturulmadıysa (ExternalService) null.</summary>
    public int? ApplicationUserId { get; set; }

    public List<IssuedCredentialResponseItem> IssuedCredentials { get; set; } = new();
}

public class IssuedCredentialResponseItem
{
    public Guid CredentialId { get; set; }
    public string CredentialType { get; set; } = null!;

    /// <summary>Sunucu tarafından otomatik üretildiyse (örn. API key) dolu - bir daha gösterilmez.</summary>
    public string? GeneratedRawValue { get; set; }
}