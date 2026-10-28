using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// Organization oluşturma REQUEST modeli. BaseModel'den türemiyor (bkz. gerekçe:
/// IndividualModel/InstrumentModel).
/// </summary>
public class CreateOrganizationModel
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Şirket adı zorunludur")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [JsonPropertyName("taxOffice")]
    [MaxLength(100)]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    [Required(ErrorMessage = "Vergi numarası zorunludur")]
    public long TaxNumber { get; set; }

    [JsonPropertyName("identityNumber")]
    public long IdentityNumber { get; set; }

    [JsonPropertyName("tradeName")]
    [MaxLength(200)]
    public string? TradeName { get; set; }

    [JsonPropertyName("tradeRegisterNumber")]
    public long TradeRegisterNumber { get; set; }

    [JsonPropertyName("mersisNo")]
    public long MersisNo { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }

    [JsonPropertyName("contactMedium")]
    public List<ContactMediumModel> ContactMedium { get; set; } = new();

    [JsonPropertyName("relatedParty")]
    public List<RelatedPartyModel> RelatedParty { get; set; } = new();
}

/// <summary>
/// Organization güncelleme (PATCH) REQUEST modeli.
/// </summary>
public class UpdateOrganizationModel
{
    [JsonPropertyName("name")]
    [MaxLength(200)]
    public string? Name { get; set; }

    [JsonPropertyName("taxOffice")]
    [MaxLength(100)]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    public long? TaxNumber { get; set; }

    [JsonPropertyName("identityNumber")]
    public long? IdentityNumber { get; set; }

    [JsonPropertyName("tradeName")]
    [MaxLength(200)]
    public string? TradeName { get; set; }

    [JsonPropertyName("tradeRegisterNumber")]
    public long? TradeRegisterNumber { get; set; }

    [JsonPropertyName("mersisNo")]
    public long? MersisNo { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }

    [JsonPropertyName("contactMedium")]
    public List<ContactMediumModel>? ContactMedium { get; set; }

    [JsonPropertyName("relatedParty")]
    public List<RelatedPartyModel>? RelatedParty { get; set; }
}
