using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// TMF632 - Individual resource model
/// </summary>
public class IndividualModel : BaseModel
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
