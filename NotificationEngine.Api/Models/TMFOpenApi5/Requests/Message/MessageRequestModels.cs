using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class MessageReceiverModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("appUserId")]
    public string? AppUserId { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
}

public class MessageSenderModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
}

public class MessageCharacteristicModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    /// <summary>TMF "Any": JSON'dan JsonElement olarak gelir.</summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}