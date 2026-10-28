using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Base;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;

namespace NotificationEngine.Infrastructure.Repositories;

/// <summary>
/// Tüm entity'ler için ortak CRUD + Include + ThenInclude + Asenkron operasyonları
/// EF Core bağımlılığı sadece burada – Core tamamen temiz kalır
/// </summary>
public class Repository<T, TKey> : IRepository<T, TKey> where T : ModelBase<TKey>
{
    protected readonly NotificationEngineDbContext _context;
    protected readonly ILogger<Repository<T, TKey>> _logger;

    public Repository(
        NotificationEngineDbContext context,
        ILogger<Repository<T, TKey>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // ── Create ────────────────────────────────────────────────────────────

    public virtual void Add(T entity)
        => _context.Set<T>().Add(entity);

    public virtual void AddRange(IEnumerable<T> entities)
        => _context.Set<T>().AddRange(entities);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _context.Set<T>().AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await _context.Set<T>().AddRangeAsync(entities, ct);

    // ── Update ────────────────────────────────────────────────────────────

    public virtual void Update(T entity)
        => _context.Set<T>().Update(entity);

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
        => _context.Set<T>().UpdateRange(entities);

    public virtual Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        _context.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    // ── Delete ────────────────────────────────────────────────────────────

    public virtual void Remove(T entity)
        => _context.Set<T>().Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities)
        => _context.Set<T>().RemoveRange(entities);

    public virtual Task RemoveAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        _context.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    // ── Get by Id ─────────────────────────────────────────────────────────

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default)
        => await _context.Set<T>().FindAsync([id], ct);

    // ── Exists ────────────────────────────────────────────────────────────

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _context.Set<T>().AnyAsync(predicate, ct);

    // ── Count ─────────────────────────────────────────────────────────────

    public virtual int Count()
        => _context.Set<T>().Count();

    public virtual int Count(Expression<Func<T, bool>> predicate)
        => _context.Set<T>().Count(predicate);

    public virtual async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Set<T>().CountAsync(ct);

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _context.Set<T>().CountAsync(predicate, ct);

    // ── Count By Group ────────────────────────────────────────────────────

    public virtual IReadOnlyDictionary<TGroupKey, int> CountByGroup<TGroupKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TGroupKey>> groupSelector) where TGroupKey : notnull
    {
        return _context.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .GroupBy(groupSelector)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
    }

    public virtual async Task<IReadOnlyDictionary<TGroupKey, int>> CountByGroupAsync<TGroupKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TGroupKey>> groupSelector,
        CancellationToken ct = default) where TGroupKey : notnull
    {
        var result = await _context.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .GroupBy(groupSelector)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return result.ToDictionary(x => x.Key, x => x.Count);
    }

    // ── Find (sync) ───────────────────────────────────────────────────────

    public virtual IReadOnlyList<T> Find(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking);
        return query.ToList();
    }

    public virtual IReadOnlyList<TResult> FindSelect<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking);
        return query.Select(selector).ToList();
    }

    // ── Find (async) ──────────────────────────────────────────────────────

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking);
        return await query.ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TResult>> FindSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking);
        return await query.Select(selector).ToListAsync(ct);
    }

    // ── Find Paged (async) ────────────────────────────────────────────────

    public virtual async Task<IReadOnlyList<T>> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking: true);
        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    // ── Find Offset (async) ──────────────────────────────────────────────

    public virtual async Task<IReadOnlyList<T>> FindOffsetAsync(
        Expression<Func<T, bool>> predicate,
        int offset,
        int limit,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default)
    {
        // Guard: negatif/sıfır değerler sessizce yanlış Skip/Take üretmesin.
        if (offset < 0) offset = 0;
        if (limit <= 0) limit = 20;

        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include, asNoTracking: true);

        return await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TResult>> FindOffsetSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        int offset,
        int limit,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default)
    {
        // Guard: negatif/sıfır değerler sessizce yanlış Skip/Take üretmesin
        // (FindOffsetAsync ile aynı kural).
        if (offset < 0) offset = 0;
        if (limit <= 0) limit = 20;

        // include YOK: Select(selector) navigation erişimini kendi başına
        // JOIN'e çevirir; ayrı bir Include gereksiz ve hatta bu metodun
        // amacına (sadece gerekli kolonları çekmek) aykırı olurdu.
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, orderBy, include: null, asNoTracking: true);

        return await query
            .Skip(offset)
            .Take(limit)
            .Select(selector)
            .ToListAsync(ct);
    }

    // ── Find After / Keyset (async) ──────────────────────────────────────

    public virtual async Task<IReadOnlyList<T>> FindAfterAsync<TCursor>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TCursor>> keySelector,
        TCursor? after,
        int limit,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default)
        where TCursor : struct
    {
        if (limit <= 0) limit = 1000;

        IQueryable<T> query = _context.Set<T>().AsNoTracking().Where(predicate);

        if (include != null) query = include(query);

        if (after.HasValue)
        {
            // WHERE {keySelector} > {after} — Skip yok, index seek.
            var param       = keySelector.Parameters[0];
            var keyBody     = keySelector.Body;
            var afterConst  = Expression.Constant(after.Value, typeof(TCursor));
            var greaterThan = Expression.GreaterThan(keyBody, afterConst);
            var whereLambda = Expression.Lambda<Func<T, bool>>(greaterThan, param);

            query = query.Where(whereLambda);
        }

        // Sıralama HER ZAMAN keySelector ile aynı kolon üzerinden olmalı — aksi
        // halde döndürülen son kaydın cursor'ı bir sonraki çağrı için anlamsızlaşır.
        query = query.OrderBy(keySelector);

        return await query.Take(limit).ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TResult>> FindAfterSelectAsync<TCursor, TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TCursor>> keySelector,
        Expression<Func<T, TResult>> selector,
        TCursor? after,
        int limit,
        CancellationToken ct = default)
        where TCursor : struct
    {
        if (limit <= 0) limit = 1000;

        // include YOK — FindOffsetSelectAsync'teki aynı gerekçe.
        IQueryable<T> query = _context.Set<T>().AsNoTracking().Where(predicate);

        if (after.HasValue)
        {
            // WHERE {keySelector} > {after} — Skip yok, index seek.
            // (FindAfterAsync ile birebir aynı expression kurulumu.)
            var param       = keySelector.Parameters[0];
            var keyBody     = keySelector.Body;
            var afterConst  = Expression.Constant(after.Value, typeof(TCursor));
            var greaterThan = Expression.GreaterThan(keyBody, afterConst);
            var whereLambda = Expression.Lambda<Func<T, bool>>(greaterThan, param);

            query = query.Where(whereLambda);
        }

        // Sıralama HER ZAMAN keySelector ile aynı kolon üzerinden olmalı —
        // aksi halde döndürülen son kaydın cursor'ı bir sonraki çağrı için
        // anlamsızlaşır (FindAfterAsync ile aynı kural).
        query = query.OrderBy(keySelector);

        return await query.Take(limit).Select(selector).ToListAsync(ct);
    }

    // ── FindOne (sync) ────────────────────────────────────────────────────

    public virtual T? FindOne(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, null, include, asNoTracking);
        return query.FirstOrDefault();
    }

    public virtual TResult? FindOneSelect<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, null, include, asNoTracking);
        return query.Select(selector).FirstOrDefault();
    }

    // ── FindOne (async) ───────────────────────────────────────────────────

    public virtual async Task<T?> FindOneAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, null, include, asNoTracking);
        return await query.FirstOrDefaultAsync(ct);
    }

    public virtual async Task<TResult?> FindOneSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default)
    {
        var query = ApplyQueryOptions(_context.Set<T>().AsQueryable(), predicate, null, include, asNoTracking);
        return await query.Select(selector).FirstOrDefaultAsync(ct);
    }

    // ── Yardımcı Metod ────────────────────────────────────────────────────

    private IQueryable<T> ApplyQueryOptions(
        IQueryable<T> query,
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
        Func<IQueryable<T>, IQueryable<T>>? include,
        bool asNoTracking)
    {
        if (asNoTracking) query = query.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        if (include != null) query = include(query);
        if (orderBy != null) query = orderBy(query);
        return query;
    }
}
