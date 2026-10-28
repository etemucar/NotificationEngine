using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

/// <summary>
/// PartyRole oluşturma REQUEST modeli. BaseModel'den türemiyor.
/// </summary>
public class CreatePartyRoleModel
{
    [JsonPropertyName("partyId")]
    [Required(ErrorMessage = "Party ID zorunludur")]
    public int PartyId { get; set; }

    [JsonPropertyName("partyRoleTypeId")]
    [Required(ErrorMessage = "Party role tipi zorunludur")]
    public int PartyRoleTypeId { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }
}

/// <summary>
/// PartyRole güncelleme (PATCH) REQUEST modeli. Not: PartyId genelde
/// değiştirilmez (yeni role atamak yerine yeni PartyRole oluşturulur) — bu
/// yüzden burada yok; sadece ValidFor gibi zaman aralığı güncellenebilir.
/// PartyRoleTypeId'nin değiştirilebilir olup olmadığı iş kuralına bağlı,
/// gerekirse eklenir.
/// </summary>
public class UpdatePartyRoleModel
{
    [JsonPropertyName("partyRoleTypeId")]
    public int PartyRoleTypeId { get; set; }

    [JsonPropertyName("validFor")]
    public TimePeriodModel? ValidFor { get; set; }
}
