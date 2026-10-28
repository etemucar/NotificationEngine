using NotificationEngine.Core.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace NotificationEngine.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationEngineDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(NotificationEngineDbContext context)
    {
        _context = context;
    }
    public bool HasActiveTransaction => _currentTransaction != null;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            return _currentTransaction;

        _currentTransaction = await _context.Database.BeginTransactionAsync();
        return _currentTransaction;            
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            await _currentTransaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            await _currentTransaction.RollbackAsync();
    }

    // await using desteği için (en temiz yol)
    public async ValueTask DisposeAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            await _currentTransaction.DisposeAsync();
        await _context.DisposeAsync();
    }
}

