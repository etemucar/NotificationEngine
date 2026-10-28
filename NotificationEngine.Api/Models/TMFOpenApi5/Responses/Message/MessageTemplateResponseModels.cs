using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class MessageTemplateModel : BaseModel
{
    /// <summary>true: tüm kuruluşların kullandığı ortak şablon.</summary>
    [JsonPropertyName("shared")] public bool Shared { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; } = null!;
    [JsonPropertyName("messageType")] public string MessageType { get; set; } = null!;
    [JsonPropertyName("culture")] public string Culture { get; set; } = null!;
    [JsonPropertyName("subject")] public string? Subject { get; set; }
    [JsonPropertyName("body")] public string Body { get; set; } = null!;
    /// <summary>active, passive...</summary>
    [JsonPropertyName("status")] public string Status { get; set; } = null!;
}

public class MessageTemplateTestResultModel
{
    [JsonPropertyName("state")] public string State { get; set; } = null!;
    [JsonPropertyName("sendTime")] public DateTimeOffset? SendTime { get; set; }
}