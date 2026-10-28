using System.Globalization;
using System.Text.Json;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;    // namespace'i projene göre düzelt
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Api.Mapping;

/// <summary>
/// TMF ↔ Service dönüşümleri. Düz alan kopyalama değil iş kuralı içerdikleri için
/// (kanal/alıcı seçimi, state çevirisi, characteristic ayrıştırma) AutoMapper'a değil buraya.
/// </summary>
public static class CommunicationMessageMappings
{
    private const string TemplateCodeKey = "templateCode";
    private const string CultureKey = "culture";

    // ── İstek: TMF create modeli → Command ────────────────────────────────
    public static CreateMessageCommand ToCommand(this CreateCommunicationMessageModel m, string idempotencyKey)
    {
        var channel = ParseChannel(m.MessageType);

        if (m.Receiver is not { Count: 1 })
            throw new InvalidInputException("Tam olarak bir 'receiver' verilmeli.");

        var r = m.Receiver[0];
        var recipient = channel switch
        {
            Channel.Email => r.Email,
            Channel.Sms => r.PhoneNumber,
            _ => r.AppUserId   // Push: cihaz token'ı appUserId'de taşınıyor (geçici karar)
        };
        if (string.IsNullOrWhiteSpace(recipient))
            throw new InvalidInputException(channel switch
            {
                Channel.Email => "E-posta için receiver.email gerekli.",
                Channel.Sms => "SMS için receiver.phoneNumber gerekli.",
                _ => "Push için receiver.appUserId gerekli."
            });

        string? templateCode = null, culture = null;
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in m.Characteristic ?? new List<MessageCharacteristicModel>())
        {
            if (string.IsNullOrWhiteSpace(c.Name)) continue;
            var value = ValueToString(c.Value);

            if (c.Name.Equals(TemplateCodeKey, StringComparison.OrdinalIgnoreCase)) templateCode = value;
            else if (c.Name.Equals(CultureKey, StringComparison.OrdinalIgnoreCase)) culture = value;
            else parameters[c.Name] = value ?? string.Empty;
        }

        return new CreateMessageCommand(
            channel, recipient.Trim(), m.Subject, m.Content, templateCode, parameters,
            ParsePriority(m.Priority), m.ScheduledSendTime, culture, idempotencyKey);
    }

    /// <summary>Anında gönderim (OTP) isteği. Ayrıştırma kuralları create ile aynıdır; idempotency anahtarı kullanılmaz.</summary>
    public static SendNowCommand ToSendNowCommand(this CreateCommunicationMessageModel m)
    {
        var c = m.ToCommand("send-now");
        return new SendNowCommand(c.Channel, c.Recipient, c.Subject, c.Content, c.TemplateCode, c.Parameters, c.Culture);
    }

    // ── Cevap: Service Response → TMF modeli ──────────────────────────────
    public static CommunicationMessageModel ToModel(this MessageResponse r, string href)
    {
        var state = ToState(r.DeliveryStatus);

        var receiver = new MessageReceiverModel
        {
            Email = r.Channel == Channel.Email ? r.Recipient : null,
            PhoneNumber = r.Channel == Channel.Sms ? r.Recipient : null,
            AppUserId = r.Channel == Channel.Push ? r.Recipient : null
        };

        var characteristic = new List<MessageCharacteristicModel>
        {
            new() { Name = "deliveryStatus", Value = r.DeliveryStatus.ToString() },
            new() { Name = "attemptCount", Value = r.AttemptCount }
        };
        if (r.TemplateCode is not null)
            characteristic.Add(new() { Name = TemplateCodeKey, Value = r.TemplateCode });

        return new CommunicationMessageModel
        {
            Id = r.Id.ToString(),
            Href = href,
            Type = "CommunicationMessage",
            Content = r.Body,
            Subject = r.Subject,
            MessageType = ToMessageType(r.Channel),
            Priority = r.Priority == MessagePriority.High ? "1" : "5",
            ScheduledSendTime = r.DeliveryStatus == MessageStatus.Pending
                ? new DateTimeOffset(DateTime.SpecifyKind(r.NextAttemptAt, DateTimeKind.Utc))
                : null,
            SendTime = r.SentAt is { } sent
                ? new DateTimeOffset(DateTime.SpecifyKind(sent, DateTimeKind.Utc))
                : null,
            State = state,
            Receiver = new List<MessageReceiverModel> { receiver },
            Characteristic = characteristic
        };
    }

    // ── State / kanal / öncelik çevirileri ────────────────────────────────
    public static string ToState(MessageStatus s) => s switch
    {
        MessageStatus.Pending => "initial",
        MessageStatus.Processing => "inProgress",
        MessageStatus.Sent or MessageStatus.Delivered => "completed",
        MessageStatus.Failed => "failed",
        _ => "initial"
    };

    /// <summary>null → filtre yok. "cancelled" henüz üretilmediği için boş küme döner.</summary>
    public static IReadOnlyList<MessageStatus>? ToStatuses(string? state) => state?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "initial" => new[] { MessageStatus.Pending },
        "inprogress" => new[] { MessageStatus.Processing },
        "completed" => new[] { MessageStatus.Sent, MessageStatus.Delivered },
        "failed" => new[] { MessageStatus.Failed },
        "cancelled" => Array.Empty<MessageStatus>(),
        _ => throw new InvalidInputException($"Geçersiz state: '{state}'.")
    };

    public static Channel ParseChannel(string? messageType)
    {
        var key = new string((messageType ?? string.Empty)
            .Where(char.IsLetter).ToArray()).ToLowerInvariant();

        return key switch
        {
            "sms" => Channel.Sms,
            "email" => Channel.Email,
            "push" or "pushnotification" or "mobileapppushnotification" => Channel.Push,
            "" => throw new InvalidInputException("'messageType' zorunlu (SMS, Email, Push)."),
            _ => throw new InvalidInputException($"Desteklenmeyen messageType: '{messageType}'.")
        };
    }

    public static string ToMessageType(Channel c) => c switch
    {
        Channel.Email => "Email",
        Channel.Sms => "SMS",
        _ => "Push"
    };

    // TMF: küçük sayı = yüksek öncelik. ≤ 2 → High, diğer her şey (ve boş) → Normal.
    private static MessagePriority ParsePriority(string? priority) =>
        int.TryParse(priority, out var n) && n <= 2 ? MessagePriority.High : MessagePriority.Normal;

    private static string? ValueToString(object? value) => value switch
    {
        null => null,
        JsonElement e => e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
}