// NotificationEngine.Service.Tests/Helpers/RepositoryMockExtensions.cs
using System.Collections;
using System.Linq.Expressions;
using NotificationEngine.Core.Base;
using NotificationEngine.Core.Repository;
using Moq;

namespace NotificationEngine.Service.Tests.Helpers;

/// <summary>
/// IRepository mock'unu bir in-memory listeye bağlar: handler'ın gönderdiği predicate
/// (ve varsa orderBy/selector) bu liste üzerinde GERÇEKTEN çalıştırılır.
///
/// Neden: "Find*(It.IsAny&lt;Expression&gt;) → şu listeyi dön" tarzı setup, handler'ın
/// predicate'ini hiç sınamaz (yanlış kullanıcı/varlık/süre filtresi testte fark edilmez).
/// Bu yardımcıyla predicate hataları da testte yakalanır.
///
/// Gereksinim: Moq 4.13+ (FindSelectAsync için It.IsAnyType / InvocationFunc).
/// </summary>
public static class RepositoryMockExtensions
{
    /// <summary>FindOneAsync + FindAsync + FindSelectAsync'i aynı listeye bağlar.</summary>
    public static void SetupInMemoryStore<T, TKey>(this Mock<IRepository<T, TKey>> mock, IEnumerable<T> store)
        where T : ModelBase<TKey>
    {
        var items = store.ToList();
        mock.SetupFindOne(items);
        mock.SetupFind(items);
        mock.SetupFindSelect(items);
    }

    public static void SetupFindOne<T, TKey>(this Mock<IRepository<T, TKey>> mock, IEnumerable<T> store)
        where T : ModelBase<TKey>
    {
        var items = store.ToList();

        mock.Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<T, bool>> predicate,
                      Func<IQueryable<T>, IQueryable<T>>? include,
                      bool asNoTracking,
                      CancellationToken ct)
                => Task.FromResult<T?>(items.AsQueryable().FirstOrDefault(predicate)));
    }

    public static void SetupFind<T, TKey>(this Mock<IRepository<T, TKey>> mock, IEnumerable<T> store)
        where T : ModelBase<TKey>
    {
        var items = store.ToList();

        mock.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IOrderedQueryable<T>>?>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<T, bool>> predicate,
                      Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
                      Func<IQueryable<T>, IQueryable<T>>? include,
                      bool asNoTracking,
                      CancellationToken ct) =>
            {
                IQueryable<T> query = items.AsQueryable().Where(predicate);
                if (orderBy is not null)
                    query = orderBy(query);

                return Task.FromResult<IReadOnlyList<T>>(query.ToList());
            });
    }

    /// <summary>
    /// FindSelectAsync&lt;TResult&gt; generic olduğu (ve handler'lar TResult olarak private nested
    /// record kullanabildiği) için It.IsAnyType ile kurulur; selector çalışma zamanında
    /// derlenip liste üzerinde uygulanır.
    /// </summary>
    public static void SetupFindSelect<T, TKey>(this Mock<IRepository<T, TKey>> mock, IEnumerable<T> store)
        where T : ModelBase<TKey>
    {
        var items = store.ToList();

        mock.Setup(x => x.FindSelectAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, It.IsAnyType>>>(),
                It.IsAny<Func<IQueryable<T>, IOrderedQueryable<T>>?>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var resultType = invocation.Method.GetGenericArguments()[0];
                var predicate = (Expression<Func<T, bool>>)invocation.Arguments[0]!;
                var selector = ((LambdaExpression)invocation.Arguments[1]!).Compile();

                var projected = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType))!;
                foreach (var item in items.AsQueryable().Where(predicate))
                    projected.Add(selector.DynamicInvoke(item));

                var readOnlyListType = typeof(IReadOnlyList<>).MakeGenericType(resultType);
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(readOnlyListType)
                    .Invoke(null, new object[] { projected })!;
            }));
    }
}