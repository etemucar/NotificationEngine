using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// TMF632 - PartyRole resource model
/// </summary>
public class PartyRoleModel : BaseModel
{
    [JsonPropertyName("partyId")]
    [Required(ErrorMessage = "Party ID zorunludur")]
    public int PartyId { get; set; }

    [JsonPropertyName("partyRoleTypeId")]
    [Required(ErrorMessage = "Party role tipi zorunludur")]
    [MaxLength(100)]
    public int PartyRoleTypeId { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }
}
