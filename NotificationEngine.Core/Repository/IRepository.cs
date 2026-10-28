using System.Linq.Expressions;
using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Repository;

public interface IRepository<T, TKey> where T : ModelBase<TKey> {
    // ── Create ────────────────────────────────────────────────────────────
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    // ── Update ────────────────────────────────────────────────────────────
    void Update(T entity);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    void UpdateRange(IEnumerable<T> entities);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    // ── Delete ────────────────────────────────────────────────────────────
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task RemoveAsync(T entity, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    // ── Get by Id ─────────────────────────────────────────────────────────
    Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default);

    // ── Exists ────────────────────────────────────────────────────────────
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    // ── Count ─────────────────────────────────────────────────────────────
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    // ── Count By Group ────────────────────────────────────────────────────
    // Trending/aggregate feed'ler için (örn. "en çok beğenilen post" — Vote
    // tablosunda TargetId'ye göre grupla, her grubun COUNT'unu al). SQL
    // seviyesinde GROUP BY + COUNT(*) üretir — tüm satırları belleğe çekip
    // C# tarafında gruplamaktan (ölçeklenmez) kaçınmak için eklendi.
    IReadOnlyDictionary<TGroupKey, int> CountByGroup<TGroupKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TGroupKey>> groupSelector
    ) where TGroupKey : notnull;

    Task<IReadOnlyDictionary<TGroupKey, int>> CountByGroupAsync<TGroupKey>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TGroupKey>> groupSelector,
        CancellationToken ct = default
    ) where TGroupKey : notnull;

    // ── Find (sync) ───────────────────────────────────────────────────────
    IReadOnlyList<T> Find(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true
    );

    IReadOnlyList<TResult> FindSelect<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true
    );

    // ── Find (async) ──────────────────────────────────────────────────────
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<TResult>> FindSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default
    );

    // ── Find Paged (async) ────────────────────────────────────────────────
    Task<IReadOnlyList<T>> FindPagedAsync(
        Expression<Func<T, bool>> predicate,
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default
    );

    // ── Find Offset (async) ──────────────────────────────────────────────
    // FindPagedAsync'in (pageNumber-1)*pageSize hesabı, offset'in pageSize'ın
    // katı olduğunu varsayar; TMF'nin offset/limit sözleşmesinde offset serbest
    // bir değer olabilir (offset=15, limit=20 gibi). Bu metod offset/limit'i
    // ARA ÇEVİRİM OLMADAN doğrudan Skip/Take'e verir.
    Task<IReadOnlyList<T>> FindOffsetAsync(
        Expression<Func<T, bool>> predicate,
        int offset,
        int limit,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default
    );

    // FindOffsetAsync ile aynı Skip/Take mantığı, ama entity yerine doğrudan
    // TResult'a projekte eder — EF Core Select() içinde SADECE selector'da
    // referans edilen kolonları SQL SELECT listesine koyar (gerçek sparse
    // fieldset / kısmi kolon çekimi). include almaz: Select projeksiyonunda
    // navigation'lara erişim EF tarafından otomatik join'e çevrilir, ayrı bir
    // Include çağrısına gerek yoktur.
    Task<IReadOnlyList<TResult>> FindOffsetSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        int offset,
        int limit,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default
    );

    // ── Find After / Keyset (async) ──────────────────────────────────────
    // Büyük veri setlerinin sıralı/toplu çekimi için: "son görülen cursor
    // değerinden sonrasını getir". OFFSET'in aksine, sıralı sonuç kümesinde
    // araya ekleme/silme olsa bile kayıt atlamaz veya tekrarlamaz.
    //
    // DİKKAT: TCursor, Expression.GreaterThan ile SQL'e çevrilebilen bir tip
    // olmalı — int, long, DateTime, decimal gibi native '>' operatörüne sahip
    // tipler için çalışır. Guid gibi native '>' operatörü OLMAYAN tiplerde
    // (örn. DigitalIdentity.Id) expression kurulumunda hata fırlatır —
    // bu tür entity'ler için kullanılmamalı.
    Task<IReadOnlyList<T>> FindAfterAsync<TCursor>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TCursor>> keySelector,
        TCursor? after,
        int limit,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        CancellationToken ct = default
    ) where TCursor : struct;

    // FindAfterAsync ile aynı keyset mantığı, ama entity yerine doğrudan
    // TResult'a projekte eder (bkz. FindOffsetSelectAsync'teki gerekçe).
    // include almaz — aynı sebep: Select projeksiyonu navigation join'lerini
    // kendi başına çözer.
    Task<IReadOnlyList<TResult>> FindAfterSelectAsync<TCursor, TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TCursor>> keySelector,
        Expression<Func<T, TResult>> selector,
        TCursor? after,
        int limit,
        CancellationToken ct = default
    ) where TCursor : struct;

    // ── FindOne (sync) ────────────────────────────────────────────────────
    T? FindOne(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true
    );

    TResult? FindOneSelect<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true
    );

    // ── FindOne (async) ───────────────────────────────────────────────────
    Task<T?> FindOneAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default
    );

    Task<TResult?> FindOneSelectAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken ct = default
    );
}
