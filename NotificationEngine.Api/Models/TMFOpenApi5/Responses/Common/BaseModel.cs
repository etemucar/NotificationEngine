using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public abstract class BaseModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("@baseType")]
    public string? BaseType { get; set; }

    [JsonPropertyName("@type")]
    public string? Type { get; set; }

    [JsonPropertyName("@schemaLocation")]
    public string? SchemaLocation { get; set; }
}
