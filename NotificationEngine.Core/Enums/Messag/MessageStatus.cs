namespace NotificationEngine.Core.Enums;

public enum MessageStatus
{
    Pending = 1,     // kuyrukta, worker bekliyor
    Processing = 2,  // worker claim etti, LockedUntil'e kadar kilitli
    Sent = 3,        // sağlayıcı kabul etti
    Delivered = 4,   // webhook ile teslim doğrulandı (her kanalda gelmeyebilir)
    Failed = 5       // deneme sınırı aşıldı veya kalıcı hata
}