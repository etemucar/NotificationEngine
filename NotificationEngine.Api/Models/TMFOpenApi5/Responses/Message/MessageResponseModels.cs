using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

// TMF681 Communication Management API v4.0.0 — ilk sürümde desteklenen alt küme.
// Desteklenmeyenler: attachment, logFlag, tryTimes, sendTimeComplete, sender (create'te yok sayılır).

public class CommunicationMessageModel : BaseModel
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("scheduledSendTime")]
    public DateTimeOffset? ScheduledSendTime { get; set; }

    [JsonPropertyName("sendTime")]
    public DateTimeOffset? SendTime { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("receiver")]
    public List<MessageReceiverModel>? Receiver { get; set; }

    [JsonPropertyName("characteristic")]
    public List<MessageCharacteristicModel>? Characteristic { get; set; }
}

/// <summary>POST gövdesi. id/href sunucu tarafından atanır, gelirse yok sayılır.</summary>
public class CreateCommunicationMessageModel : BaseModel
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("scheduledSendTime")]
    public DateTimeOffset? ScheduledSendTime { get; set; }

    [JsonPropertyName("receiver")]
    public List<MessageReceiverModel>? Receiver { get; set; }

    [JsonPropertyName("sender")]
    public MessageSenderModel? Sender { get; set; }

    [JsonPropertyName("characteristic")]
    public List<MessageCharacteristicModel>? Characteristic { get; set; }
}