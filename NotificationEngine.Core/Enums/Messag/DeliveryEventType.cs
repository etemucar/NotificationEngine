namespace NotificationEngine.Core.Enums;

public enum DeliveryEventType
{
    Attempted = 1,        // worker gönderim denedi (başarılı ya da değil, Detail'de)
    Accepted = 2,         // sağlayıcı isteği kabul etti
    Delivered = 3,        // webhook: teslim edildi
    Bounced = 4,          // webhook: geri döndü
    Complaint = 5,        // webhook: spam şikayeti
    InvalidRecipient = 6, // geçersiz FCM token, olmayan numara/adres
    Failed = 7            // deneme sınırı aşıldı
}