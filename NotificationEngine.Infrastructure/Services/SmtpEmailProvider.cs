using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;

namespace NotificationEngine.Infrastructure;

/// <summary>
/// SMTP (MailKit). Her gönderimde kısa ömürlü bir bağlantı açar (düşük hacim için yeterli).
/// Alıcı adresi, konu ve gövde loglanmaz.
/// </summary>
public sealed class SmtpEmailProvider : IChannelProvider
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(SmtpOptions options, ILogger<SmtpEmailProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Channel Channel => Channel.Email;
    public string Name => "Smtp";

    public async Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return SendResult.Permanent("SMTP yapılandırması eksik (Host, FromAddress).");

        if (!MailboxAddress.TryParse(message.Recipient, out var to))
            return SendResult.InvalidRecipient("Alıcı e-posta adresi geçersiz.");

        var fromAddress = message.FromEmail ?? _options.FromAddress!;
        var fromName = Sanitize(message.FromName ?? _options.FromName);

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(fromName, fromAddress));
        mime.To.Add(to);
        mime.Subject = message.Subject ?? string.Empty;
        mime.Body = new TextPart(
            _options.BodyFormat.Equals("Html", StringComparison.OrdinalIgnoreCase) ? "html" : "plain")
        {
            Text = message.Body
        };

        try
        {
            using var client = new SmtpClient { Timeout = 15_000 };
            await client.ConnectAsync(_options.Host!, _options.Port, ParseSecurity(_options.Security), ct);

            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);

            var serverResponse = await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            return SendResult.Ok(serverResponse);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (AuthenticationException)
        {
            _logger.LogError("SMTP kimlik doğrulama hatası. Kullanıcı adı/şifre ve sunucu ayarlarını kontrol edin.");
            return SendResult.Permanent("SMTP kimlik doğrulaması başarısız.");
        }
        catch (SmtpCommandException ex)
        {
            var code = (int)ex.StatusCode;
            if (ex.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
                return SendResult.InvalidRecipient($"SMTP alıcıyı kabul etmedi ({code}).");

            // 4xx geçici (gri liste, yoğunluk), 5xx kalıcı.
            return code is >= 400 and < 500
                ? SendResult.Transient($"SMTP geçici hata ({code}).")
                : SendResult.Permanent($"SMTP hata kodu: {code}.");
        }
        catch (Exception ex) when (ex is SmtpProtocolException or ServiceNotConnectedException
                                       or IOException or System.Net.Sockets.SocketException
                                       or TimeoutException or TaskCanceledException)
        {
            _logger.LogWarning("SMTP sunucusuna ulaşılamadı: {ExceptionType}", ex.GetType().Name);
            return SendResult.Transient("SMTP sunucusuna ulaşılamadı.");
        }
    }

    private static SecureSocketOptions ParseSecurity(string value) => value.ToLowerInvariant() switch
    {
        "sslonconnect" => SecureSocketOptions.SslOnConnect,
        "auto" => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.StartTls
    };

    // Görünen ad başlığa girer: satır sonu ve adres ayraçlarını at (başlık enjeksiyonu).
    private static string Sanitize(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : new string(name.Where(ch => ch is not ('\r' or '\n' or '<' or '>' or '"')).ToArray()).Trim();
}