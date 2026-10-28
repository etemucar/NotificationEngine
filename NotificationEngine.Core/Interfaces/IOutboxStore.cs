using NotificationEngine.Core.Model;

namespace NotificationEngine.Core.Interfaces;

/// <summary>
/// Generic IRepository'nin sunmadığı outbox işlemleri (ham SQL / FOR UPDATE SKIP LOCKED).
/// Implementasyon Infrastructure'da.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Kısa bir transaction içinde: NextAttemptAt &lt;= now olan Pending mesajları (ve LockedUntil
    /// süresi dolmuş Processing mesajları) FOR UPDATE SKIP LOCKED ile seçer, DeliveryStatus=Processing ve
    /// LockedUntil=now+lockDuration yapıp döner. Sağlayıcı çağrısı bu transaction'ın DIŞINDA yapılır.
    /// High öncelikli mesajlar önce gelir.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimDueAsync(
        int batchSize, TimeSpan lockDuration, DateTime now, CancellationToken ct = default);
}