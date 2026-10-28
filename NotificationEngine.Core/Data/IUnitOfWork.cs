using Microsoft.EntityFrameworkCore.Storage;

namespace NotificationEngine.Core.Data;

public interface IUnitOfWork
{

    bool HasActiveTransaction { get; }    

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    ValueTask DisposeAsync(CancellationToken cancellationToken = default);

}
