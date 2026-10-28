using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class TimePeriodModel
{
    [JsonPropertyName("startDateTime")]
    public DateTime? StartDateTime { get; set; }

    [JsonPropertyName("endDateTime")]
    public DateTime? EndDateTime { get; set; }
}
