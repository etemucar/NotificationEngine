using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class ContactCharacteristicConverter : JsonConverter<ContactCharacteristicBase>
{
    public override ContactCharacteristicBase? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var type = root.TryGetProperty("@type", out var typeProp)
            ? typeProp.GetString()
            : null;

        ContactCharacteristicBase characteristic = type switch
        {
            "EmailAddress"    => root.Deserialize<EmailCharacteristic>(options)!,
            "TelephoneNumber" => root.Deserialize<TelephoneCharacteristic>(options)!,
            "PostalAddress"   => root.Deserialize<PostalAddressCharacteristic>(options)!,
            _ => throw new JsonException(
                $"Bilinmeyen ContactCharacteristic tipi: '{type}'. " +
                $"Geçerli tipler: EmailAddress, TelephoneNumber, PostalAddress")
        };

        return characteristic;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ContactCharacteristicBase value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
