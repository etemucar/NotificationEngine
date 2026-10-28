using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>
/// İleti Merkezi JSON API (POST /v1/send-sms/json).
/// Yanıt: { "response": { "status": { "code": "200", "message": "..." }, "order": { "id": "..." } } }.
/// Alıcı numarası, mesaj gövdesi, key ve hash asla loglanmaz.
/// </summary>
public sealed class IletiMerkeziSmsProvider : IChannelProvider
{
    public const string HttpClientName = "iletimerkezi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IletiMerkeziOptions _options;
    private readonly ILogger<IletiMerkeziSmsProvider> _logger;

    public IletiMerkeziSmsProvider(
        IHttpClientFactory httpClientFactory, IletiMerkeziOptions options, ILogger<IletiMerkeziSmsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public Channel Channel => Channel.Sms;
    public string Name => "IletiMerkezi";

    public async Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return SendResult.Permanent("İleti Merkezi yapılandırması eksik (Key, Secret, Sender).");

        var order = new Dictionary<string, object>
        {
            ["sender"] = _options.Sender!,
            ["message"] = new
            {
                text = message.Body,
                receipents = new { number = new[] { SmsNumber.Normalize(message.Recipient) } }
            },
            ["iys"] = _options.Iys
        };
        if (_options.Iys == "1")
            order["iysList"] = _options.IysList;

        var payload = new
        {
            request = new
            {
                authentication = new { key = _options.Key, hash = ComputeHash(_options.Key!, _options.Secret!) },
                order
            }
        };

        (HttpStatusCode Status, string Body) reply;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(_options.SendUrl, payload, ct);
            reply = (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("İleti Merkezi'ne ulaşılamadı: {ExceptionType}", ex.GetType().Name);
            return SendResult.Transient("İleti Merkezi'ne ulaşılamadı.");
        }

        string? code = null, orderId = null;
        try
        {
            using var doc = JsonDocument.Parse(reply.Body);
            if (doc.RootElement.TryGetProperty("response", out var resp))
            {
                if (resp.TryGetProperty("status", out var status))
                    code = ReadString(status, "code");
                if (resp.TryGetProperty("order", out var orderEl))
                    orderId = ReadString(orderEl, "id");
            }
        }
        catch (JsonException) { /* gövde JSON değil: aşağıda HTTP durumuna göre karar verilir */ }

        if (code is null)
        {
            var transient = (int)reply.Status >= 500 || reply.Status == HttpStatusCode.TooManyRequests;
            return transient
                ? SendResult.Transient($"İleti Merkezi HTTP {(int)reply.Status}.")
                : SendResult.Permanent($"İleti Merkezi beklenmeyen yanıt (HTTP {(int)reply.Status}).");
        }

        switch (code)
        {
            case "200":
                return SendResult.Ok(orderId);
            case "452":   // alıcı numarası eksik / doğrulanamadı
                return SendResult.InvalidRecipient($"İleti Merkezi hata kodu: {code}.");
            case "401" or "402" or "450":
                // Kimlik/IP, bakiye, onaysız başlık: yapılandırma ya da hesap sorunu, yeniden denemek işe yaramaz.
                _logger.LogError("İleti Merkezi hesap/yapılandırma hatası, kod {Code}.", code);
                return SendResult.Permanent($"İleti Merkezi hata kodu: {code}.");
            default:
                // 400 istek çözülemedi, 404 yanlış adres, 451 tekrarlı sipariş, 453 metin çok uzun, 454 metin boş...
                _logger.LogWarning("İleti Merkezi hata kodu: {Code}", code);
                return SendResult.Permanent($"İleti Merkezi hata kodu: {code}.");
        }
    }

    // TEYİT ET: Dokümanlar hash'in "API anahtarı ve gizli anahtar kullanılarak üretildiğini" söylüyor,
    // algoritmayı açıkça yazmıyor. Burada HMAC-SHA256 (anahtar: gizli anahtar, veri: API anahtarı),
    // küçük harf hex kullanıldı. Panel dokümanındaki örnekle karşılaştırın; 401 alırsanız ilk bakılacak yer burası.
    private static string ComputeHash(string key, string secret) =>
        Convert.ToHexString(HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secret),
            System.Text.Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var p)
            ? (p.ValueKind == JsonValueKind.String ? p.GetString() : p.GetRawText())
            : null;
}