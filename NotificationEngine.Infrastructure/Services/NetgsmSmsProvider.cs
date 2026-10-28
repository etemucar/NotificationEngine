using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>
/// Netgsm REST v2 (POST /sms/rest/v2/send, HTTP Basic: usercode:password).
/// Yanıt: { "code": "00", "jobid": "...", "description": "..." }.
/// Alıcı numarası, mesaj gövdesi ve kimlik bilgisi asla loglanmaz.
/// </summary>
public sealed class NetgsmSmsProvider : IChannelProvider
{
    public const string HttpClientName = "netgsm";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NetgsmOptions _options;
    private readonly ILogger<NetgsmSmsProvider> _logger;

    public NetgsmSmsProvider(
        IHttpClientFactory httpClientFactory, NetgsmOptions options, ILogger<NetgsmSmsProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public Channel Channel => Channel.Sms;
    public string Name => "Netgsm";

    public async Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return SendResult.Permanent("Netgsm yapılandırması eksik (UserCode, Password, MsgHeader).");

        var payload = new Dictionary<string, object>
        {
            ["msgheader"] = _options.MsgHeader!,
            ["encoding"] = _options.MessageEncoding,
            ["messages"] = new[] { new { msg = message.Body, no = SmsNumber.Normalize(message.Recipient) } }
        };
        if (!string.IsNullOrWhiteSpace(_options.IysFilter))
            payload["iysfilter"] = _options.IysFilter!;

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.SendUrl)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.UserCode}:{_options.Password}")));

        (HttpStatusCode Status, string Body) reply;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, ct);
            reply = (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("Netgsm'e ulaşılamadı: {ExceptionType}", ex.GetType().Name);
            return SendResult.Transient("Netgsm'e ulaşılamadı.");
        }

        string? code = null, jobId = null;
        try
        {
            using var doc = JsonDocument.Parse(reply.Body);
            code = ReadString(doc.RootElement, "code");
            jobId = ReadString(doc.RootElement, "jobid");
        }
        catch (JsonException) { /* gövde JSON değil: aşağıda HTTP durumuna göre karar verilir */ }

        if (code is null)
        {
            var transient = (int)reply.Status >= 500 || reply.Status == HttpStatusCode.TooManyRequests;
            return transient
                ? SendResult.Transient($"Netgsm HTTP {(int)reply.Status}.")
                : SendResult.Permanent($"Netgsm beklenmeyen yanıt (HTTP {(int)reply.Status}).");
        }

        // 00/01/02 başarı. 30 = kimlik bilgisi / API yetkisi / IP kısıtı, 40 = başlık tanımsız:
        // bunlar yapılandırma hatasıdır, yeniden denemek işe yaramaz.
        switch (code)
        {
            case "00" or "01" or "02":
                return SendResult.Ok(jobId);
            case "80" or "85":
                return SendResult.Transient($"Netgsm gönderim sınırı (kod {code}).");
            case "30" or "40":
                _logger.LogError("Netgsm yapılandırma hatası, kod {Code}. Kimlik bilgisi, API yetkisi/IP kısıtı ve onaylı başlığı kontrol edin.", code);
                return SendResult.Permanent($"Netgsm hata kodu: {code}.");
            default:
                _logger.LogWarning("Netgsm hata kodu: {Code}", code);
                return SendResult.Permanent($"Netgsm hata kodu: {code}.");
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var p)
            ? (p.ValueKind == JsonValueKind.String ? p.GetString() : p.GetRawText())
            : null;
}