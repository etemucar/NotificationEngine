using Microsoft.EntityFrameworkCore;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Infrastructure;

public sealed class OutboxStore : IOutboxStore
{
    // DbContext sınıf adını projendeki gerçek adla değiştir.
    private readonly NotificationEngineDbContext _db;

    public OutboxStore(NotificationEngineDbContext db) => _db = db;

    public async Task<IReadOnlyList<OutboxMessage>> ClaimDueAsync(
        int batchSize, TimeSpan lockDuration, DateTime now, CancellationToken ct = default)
    {
        // Dışarıda açık bir transaction varsa ona katıl, yoksa kısa bir tane aç.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;

        try
        {
            // HAM SQL: otomatik snake_case dönüşümü içine işlemez, adlar elle yazıldı.
            // Şema = EntityConfigurationBase.Schema ("NotificationEngine") → notification_engine
            // Id kolonu = {tablo}_id. 1 = Pending, 2 = Processing; priority 2 = High (önce gelir).
            var messages = await _db.OutboxMessage
                .FromSql($"""
                    SELECT * FROM notification_engine.outbox_message
                    WHERE is_deleted <> 1
                      AND next_attempt_at <= {now}
                      AND (delivery_status = 1
                           OR (delivery_status = 2 AND locked_until < {now}))
                    ORDER BY priority DESC, next_attempt_at
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .IgnoreQueryFilters() // is_deleted yukarıda elle filtrelendi
                .ToListAsync(ct);

            foreach (var m in messages)
            {
                m.DeliveryStatus = MessageStatus.Processing;
                m.LockedUntil = now.Add(lockDuration);
                m.AttemptCount++; // claim anında sayılır: worker çökerse de deneme sayılmış olur
            }

            await _db.SaveChangesAsync(ct); // AuditInterceptor UpdateDate'i doldurur
            if (tx is not null) await tx.CommitAsync(ct);

            return messages;
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }
}