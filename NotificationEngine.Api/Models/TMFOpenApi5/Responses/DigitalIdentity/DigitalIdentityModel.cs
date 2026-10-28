using System.ComponentModel.DataAnnotations;
using NotificationEngine.Core.Enums;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class DigitalIdentityModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [Required]
    [JsonPropertyName("partyRoleId")]
    public int PartyRoleId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("credential")]
    public List<CredentialModel> Credentials { get; set; } = new();
}

public class CredentialModel
{
    [Required]
    [JsonPropertyName("credentialType")]
    public CredentialType CredentialType { get; set; }

    [JsonPropertyName("trustLevel")]
    public int? TrustLevel { get; set; }

    [JsonPropertyName("characteristic")]
    public List<CredentialCharacteristicModel> Characteristics { get; set; } = new();

    [JsonPropertyName("contactMedium")]
    public List<ContactMediumModel> ContactMedia { get; set; } = new();
}

public class CredentialCharacteristicModel
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [Required]
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}
