using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationEngine.Api.Models.TMFOpenApi5;

public class CreateMessageTemplateModel
{
    [JsonPropertyName("code")]
    [Required(ErrorMessage = "'code' zorunlu.")]
    [MaxLength(100)]
    public string Code { get; set; } = null!;

    /// <summary>SMS, Email ya da Push.</summary>
    [JsonPropertyName("messageType")]
    [Required(ErrorMessage = "'messageType' zorunlu (SMS, Email, Push).")]
    public string MessageType { get; set; } = null!;

    /// <summary>Boşsa varsayılan kültür (tr-TR).</summary>
    [JsonPropertyName("culture")]
    [MaxLength(10)]
    public string? Culture { get; set; }

    [JsonPropertyName("subject")]
    [MaxLength(200)]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    [Required(ErrorMessage = "'body' zorunlu.")]
    public string Body { get; set; } = null!;
}

/// <summary>
/// PATCH (merge-patch) modeli: gönderilmeyen alana dokunulmaz. Kod, kanal ve kültür (benzersiz anahtar) değişmez.
/// </summary>
public class UpdateMessageTemplateModel
{
    [JsonPropertyName("subject")]
    [MaxLength(200)]
    public string? Subject { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>"active" ya da "passive". Pasif şablon gönderimde kullanılmaz, silinmez.</summary>
    [JsonPropertyName("status")]
    [MaxLength(20)]
    public string? Status { get; set; }
}

public class TestMessageTemplateModel
{
    /// <summary>SMS için +905551112233 biçiminde telefon, e-posta için adres.</summary>
    [JsonPropertyName("recipient")]
    [Required(ErrorMessage = "'recipient' zorunlu.")]
    public string Recipient { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string>? Parameters { get; set; }
}

