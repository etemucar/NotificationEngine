using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class PartyOrPartyRoleModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("href")]
    public string Href { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "RelatedParty";

    [JsonPropertyName("@referredType")]
    public string ReferredType { get; set; } = null!;
}

public class RelatedPartyModel
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("@type")]
    public string Type { get; set; } = "RelatedPartyRefOrPartyRoleRef";

    [JsonPropertyName("partyOrPartyRole")]
    public PartyOrPartyRoleModel PartyOrPartyRole { get; set; } = new();
}    
