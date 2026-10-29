using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>
/// Amazon SES v2 (SendEmail). Alıcı adresi, konu ve gövde loglanmaz; hata sonucuna yalnızca AWS hata kodu girer.
/// NOT: SES, alıcı adresinin gerçekten var olup olmadığını gönderim anında bilmez; geri dönen/şikayet edilen
/// adresler sonradan (SNS/olay hedefi ile) bildirilir. Bu yüzden InvalidRecipient burada üretilmez.
/// </summary>
public sealed class SesEmailProvider : IChannelProvider
{
    private readonly SesOptions _options;
    private readonly ILogger<SesEmailProvider> _logger;
    private readonly Lazy<AmazonSimpleEmailServiceV2Client> _client;

    public SesEmailProvider(SesOptions options, ILogger<SesEmailProvider> logger)
    {
        _options = options;
        _logger = logger;
        // İstemci ilk gönderimde oluşturulur: yapılandırması eksik olan sağlayıcı açılışta patlamasın.
        // PublicationOnly: oluşturma sırasında fırlayan hata önbelleğe alınmaz, sonraki çağrıda yeniden denenir.
        _client = new Lazy<AmazonSimpleEmailServiceV2Client>(CreateClient, LazyThreadSafetyMode.PublicationOnly);
    }

    public Channel Channel => Channel.Email;
    public string Name => "Ses";

    public async Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return SendResult.Permanent("SES yapılandırması eksik (Region, FromAddress).");

        // Anahtarlardan yalnızca biri doluysa sessizce varsayılan AWS kimlik zincirine düşmek yerine açık hata ver.
        var hasKey = !string.IsNullOrWhiteSpace(_options.AccessKeyId);
        var hasSecret = !string.IsNullOrWhiteSpace(_options.SecretAccessKey);
        if (hasKey != hasSecret)
        {
            _logger.LogError("SES yapılandırma hatası: AccessKeyId ve SecretAccessKey birlikte tanımlanmalı (AccessKeyId dolu: {HasKey}, SecretAccessKey dolu: {HasSecret}).",
                hasKey, hasSecret);
            return SendResult.Permanent("SES yapılandırması hatalı (AccessKeyId ve SecretAccessKey birlikte tanımlanmalı).");
        }

        var fromAddress = message.FromEmail ?? _options.FromAddress!;
        var fromName = Sanitize(message.FromName ?? _options.FromName);

        var content = new Content { Data = message.Body, Charset = "UTF-8" };
        var body = _options.BodyFormat.Equals("Html", StringComparison.OrdinalIgnoreCase)
            ? new Body { Html = content }
            : new Body { Text = content };

        var request = new SendEmailRequest
        {
            FromEmailAddress = string.IsNullOrEmpty(fromName) ? fromAddress : $"{fromName} <{fromAddress}>",
            Destination = new Destination { ToAddresses = new List<string> { message.Recipient } },
            Content = new EmailContent
            {
                Simple = new Amazon.SimpleEmailV2.Model.Message
                {
                    Subject = new Content { Data = message.Subject ?? string.Empty, Charset = "UTF-8" },
                    Body = body
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(_options.ConfigurationSet))
            request.ConfigurationSetName = _options.ConfigurationSet;

        try
        {
            var response = await _client.Value.SendEmailAsync(request, ct);
            return SendResult.Ok(response.MessageId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (TooManyRequestsException)
        {
            return SendResult.Transient("SES istek sınırı (TooManyRequests).");
        }
        catch (LimitExceededException)
        {
            return SendResult.Transient("SES gönderim kotası (LimitExceeded).");
        }
        catch (AmazonServiceException ex) when ((int)ex.StatusCode >= 500)
        {
            _logger.LogWarning("SES sunucu hatası: {ErrorCode}", ex.ErrorCode);
            return SendResult.Transient($"SES hata kodu: {ex.ErrorCode}.");
        }
        catch (AmazonServiceException ex)
        {
            // MessageRejected, MailFromDomainNotVerified, AccountSuspended, BadRequest...: yeniden denemek işe yaramaz.
            // En sık sebep: gönderen adres/alan adı SES'te doğrulanmamış ya da hesap hâlâ sandbox'ta.
            _logger.LogError("SES reddetti: {ErrorCode}", ex.ErrorCode);
            return SendResult.Permanent($"SES hata kodu: {ex.ErrorCode}.");
        }
        catch (Exception ex) when (ex is AmazonClientException or HttpRequestException or TaskCanceledException)
        {
            // Kök neden teşhisi için hata mesajı ve iç hata tipi loglanır (ör. "Unable to find credentials",
            // "No RegionEndpoint or ServiceURL configured"). Bu mesajlar alıcı adresi, konu ya da gövde içermez.
            _logger.LogWarning(
                "SES'e ulaşılamadı: {ExceptionType} - {Message} (iç hata: {InnerType} - {InnerMessage})",
                ex.GetType().Name,
                ex.Message,
                ex.InnerException?.GetType().Name ?? "-",
                ex.InnerException?.Message ?? "-");
            return SendResult.Transient("SES'e ulaşılamadı.");
        }
    }

    private AmazonSimpleEmailServiceV2Client CreateClient()
    {
        var region = RegionEndpoint.GetBySystemName(_options.Region);

        return !string.IsNullOrWhiteSpace(_options.AccessKeyId) && !string.IsNullOrWhiteSpace(_options.SecretAccessKey)
            ? new AmazonSimpleEmailServiceV2Client(
                new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey), region)
            : new AmazonSimpleEmailServiceV2Client(region);   // varsayılan AWS kimlik zinciri
    }

    // Görünen ad başlığa girer: satır sonu ve adres ayraçlarını at (başlık enjeksiyonu).
    private static string Sanitize(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : new string(name.Where(ch => ch is not ('\r' or '\n' or '<' or '>' or '"')).ToArray()).Trim();
}