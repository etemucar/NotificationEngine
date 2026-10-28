using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// Individual oluşturma REQUEST modeli. BaseModel'den TÜREMİYOR — Id/Href/@type/
/// @baseType/@schemaLocation sunucunun ürettiği alanlardır (bkz. InstrumentModel'de
/// aynı ayrımın gerekçesi).
/// </summary>
public class CreateIndividualModel
{
    [JsonPropertyName("givenName")]
    [Required(ErrorMessage = "Ad zorunludur")]
    [MaxLength(100)]
    public string GivenName { get; set; } = null!;

    [JsonPropertyName("familyName")]
    [Required(ErrorMessage = "Soyad zorunludur")]
    [MaxLength(100)]
    public string FamilyName { get; set; } = null!;

    [JsonPropertyName("middleName")]
    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [JsonPropertyName("title")]
    [MaxLength(50)]
    public string? Title { get; set; }

    [JsonPropertyName("gender")]
    [MaxLength(20)]
    public string? Gender { get; set; }

    [JsonPropertyName("nationality")]
    [MaxLength(50)]
    public string? Nationality { get; set; }

    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }

    [JsonPropertyName("deathDate")]
    public DateTime? DeathDate { get; set; }

    [JsonPropertyName("placeOfBirth")]
    [MaxLength(100)]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("countryOfBirth")]
    [MaxLength(100)]
    public string? CountryOfBirth { get; set; }

    [JsonPropertyName("maritalStatus")]
    [MaxLength(50)]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }

    [JsonPropertyName("contactMedium")]
    public List<ContactMediumModel> ContactMedium { get; set; } = new();

    [JsonPropertyName("relatedParty")]
    public List<RelatedPartyModel> RelatedParty { get; set; } = new();
}

/// <summary>
/// Individual güncelleme (PATCH) REQUEST modeli. Tüm alanlar nullable/opsiyonel —
/// gönderilmeyen alana dokunulmaz. Aynı gerekçeyle BaseModel'den türemiyor.
/// </summary>
public class UpdateIndividualModel
{
    [JsonPropertyName("givenName")]
    [MaxLength(100)]
    public string? GivenName { get; set; }

    [JsonPropertyName("familyName")]
    [MaxLength(100)]
    public string? FamilyName { get; set; }

    [JsonPropertyName("middleName")]
    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [JsonPropertyName("title")]
    [MaxLength(50)]
    public string? Title { get; set; }

    [JsonPropertyName("gender")]
    [MaxLength(20)]
    public string? Gender { get; set; }

    [JsonPropertyName("nationality")]
    [MaxLength(50)]
    public string? Nationality { get; set; }

    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }

    [JsonPropertyName("deathDate")]
    public DateTime? DeathDate { get; set; }

    [JsonPropertyName("placeOfBirth")]
    [MaxLength(100)]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("countryOfBirth")]
    [MaxLength(100)]
    public string? CountryOfBirth { get; set; }

    [JsonPropertyName("maritalStatus")]
    [MaxLength(50)]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }

    [JsonPropertyName("contactMedium")]
    public List<ContactMediumModel>? ContactMedium { get; set; }

    [JsonPropertyName("relatedParty")]
    public List<RelatedPartyModel>? RelatedParty { get; set; }
}
