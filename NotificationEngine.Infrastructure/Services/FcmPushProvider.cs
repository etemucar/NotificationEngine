using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Infrastructure.Service;

namespace NotificationEngine.Infrastructure;

/// <summary>
/// Firebase Cloud Messaging (Firebase Admin SDK). Alıcı = cihaz token'ı (TMF'de receiver.appUserId içinde taşınıyor).
/// Token, başlık ve gövde loglanmaz.
/// </summary>
public sealed class FcmPushProvider : IChannelProvider
{
    private const string AppName = "notification-engine";
    private static readonly object Gate = new();

    private readonly FcmProviderOptions  _options;
    private readonly ILogger<FcmPushProvider> _logger;

    public FcmPushProvider(FcmProviderOptions options, ILogger<FcmPushProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Channel Channel => Channel.Push;
    public string Name => "Fcm";

    public async Task<SendResult> SendAsync(RenderedMessage message, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return SendResult.Permanent("FCM yapılandırması eksik (CredentialsJson ya da CredentialsFile).");

#pragma warning disable CS0618 // Message.Token: SDK Fid öneriyor; kayıt token'ı hâlâ destekleniyor. İstemci FID'e geçerse Fid kullanılacak.
        var fcmMessage = new FirebaseAdmin.Messaging.Message
        {
            Token = message.Recipient,
            Notification = new Notification { Title = message.Subject, Body = message.Body }
        };
#pragma warning restore CS0618
        if (message.Priority == MessagePriority.High)
            fcmMessage.Android = new AndroidConfig { Priority = Priority.High };

        try
        {
            var messageId = await GetMessaging().SendAsync(fcmMessage, ct);
            return SendResult.Ok(messageId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (FirebaseMessagingException ex)
        {
            switch (ex.MessagingErrorCode)
            {
                case MessagingErrorCode.Unregistered:
                    // Uygulama silinmiş ya da token geçersiz: bu cihaza bir daha gönderilmemeli.
                    return SendResult.InvalidRecipient("FCM token kayıtlı değil (Unregistered).");

                case MessagingErrorCode.QuotaExceeded:
                case MessagingErrorCode.Unavailable:
                case MessagingErrorCode.Internal:
                    return SendResult.Transient($"FCM geçici hata: {ex.MessagingErrorCode}.");

                case MessagingErrorCode.SenderIdMismatch:
                case MessagingErrorCode.ThirdPartyAuthError:
                    _logger.LogError("FCM kimlik/proje hatası: {ErrorCode}. Servis hesabı ve proje eşleşmesini kontrol edin.",
                        ex.MessagingErrorCode);
                    return SendResult.Permanent($"FCM hata kodu: {ex.MessagingErrorCode}.");

                default:
                    _logger.LogWarning("FCM hata kodu: {ErrorCode}", ex.MessagingErrorCode);
                    return SendResult.Permanent($"FCM hata kodu: {ex.MessagingErrorCode?.ToString() ?? "bilinmiyor"}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning("FCM'e ulaşılamadı: {ExceptionType}", ex.GetType().Name);
            return SendResult.Transient("FCM'e ulaşılamadı.");
        }
    }

    // FirebaseApp süreç başına bir kez oluşturulur; aynı adla ikinci Create istisna fırlatır.
    private FirebaseMessaging GetMessaging()
    {
        lock (Gate)
        {
            var app = FirebaseApp.GetInstance(AppName) ?? FirebaseApp.Create(new AppOptions
            {
                // Yalnızca servis hesabı JSON'u kabul edilir (GoogleCredential.FromJson/FromFile kullanımdan kalkıyor).
                Credential = !string.IsNullOrWhiteSpace(_options.CredentialsJson)
                    ? CredentialFactory.FromJson<ServiceAccountCredential>(_options.CredentialsJson).ToGoogleCredential()
                    : CredentialFactory.FromFile<ServiceAccountCredential>(_options.CredentialsFile!).ToGoogleCredential(),
                ProjectId = _options.ProjectId
            }, AppName);

            return FirebaseMessaging.GetMessaging(app);
        }
    }
}