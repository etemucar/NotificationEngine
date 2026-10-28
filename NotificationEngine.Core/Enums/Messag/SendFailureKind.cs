namespace NotificationEngine.Core.Enums;

public enum SendFailureKind
{
    None = 0,
    Transient = 1,        // zaman aşımı, 5xx, rate limit → yeniden denenir
    Permanent = 2,        // 4xx, kimlik hatası, bakiye yok → yeniden denenmez
    InvalidRecipient = 3  // alıcı geçersiz → yeniden denenmez, tenant'a bildirilmeli
}