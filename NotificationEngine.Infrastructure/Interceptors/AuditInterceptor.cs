using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Base;

/// <summary>
/// CreateDate/CreatedBy/UpdateDate/UpdatedBy alanlarını SaveChanges sırasında doldurur.
///
/// ÖNEMLİ: ICurrentUserService constructor'dan ALINMAZ. CurrentUserService → IRepository →
/// DbContext → (AddDbContext lambda'sı) → AuditInterceptor → ICurrentUserService şeklinde
/// döngüsel bir bağımlılık oluşur ve DI istek sırasında sonsuza kadar bekler.
/// Servis, SaveChanges çağrıldığı anda (DbContext zaten oluşmuşken) tembel çözülür.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public AuditInterceptor(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAudit(DbContext? context)
    {
        if (context == null) return;

        // Tembel çözüm: döngüsel bağımlılığı kıran nokta.
        var currentUserService = _serviceProvider.GetRequiredService<ICurrentUserService>();
        var currentUserId = currentUserService.UserId ?? 0;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<ModelBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateDate = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.UpdateDate = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdateDate = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }
    }
}