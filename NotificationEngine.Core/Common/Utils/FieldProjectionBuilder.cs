using System.Linq.Expressions;
using System.Reflection;

namespace NotificationEngine.Core.Common;

/// <summary>
/// Herhangi bir <c>{Entity}</c> / <c>{Domain}Response</c> çifti için, projenin
/// "property adları örtüşen basit dönüşümler AutoMapper'a bırakılır" kuralına
/// dayanarak (bkz. BackendContext.md → "Katman Mimarisi") reflection ile
/// dinamik bir Expression&lt;Func&lt;TEntity, TResponse&gt;&gt; kurar. Bu, her
/// domain için ayrı bir "InstrumentProjectionBuilder" yazılmasının önüne geçer.
///
/// EF Core, Select() içinde SADECE bağlanan (bind edilen) property'leri SQL
/// SELECT listesine koyar — yani fields=symbol için gerçekten
/// "SELECT id, symbol FROM instrument" çalışır.
///
/// Karşılığı entity'de olmayan response alanları (örn. hesaplanan alanlar,
/// ya da Instrument.Detail gibi polimorfik/çoklu-navigation alanlar) sessizce
/// atlanır — bunlar için <paramref name="excludedIfRequested"/> parametresiyle
/// "bu alan istenirse hiç projeksiyon kurma, çağıran eski Include+full-map
/// yoluna düşsün" denebilir.
/// </summary>
public static class FieldProjectionBuilder
{
    public static Expression<Func<TEntity, TResponse>>? Build<TEntity, TResponse>(
        FieldSet fields,
        params string[] excludedIfRequested)
        where TResponse : new()
    {
        // fields boşsa (tüm alanlar isteniyor) sparse projeksiyon anlamsız —
        // çağıran eski tam-yükleme yoluna düşmeli.
        if (fields.IsAll)
            return null;

        // İstenen alanlardan biri "excluded" ise (örn. Instrument için "detail")
        // bu builder'ın karşılayamayacağı bir alan demektir — null dönüp
        // çağırana eski Include+full-map yolunu bırakıyoruz.
        if (excludedIfRequested.Any(fields.Contains))
            return null;

        var entityProps = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var responseProps = typeof(TResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var param = Expression.Parameter(typeof(TEntity), "x");
        var bindings = new List<MemberBinding>();

        foreach (var responseProp in responseProps)
        {
            // Id zorunlu zarf alanı — fields ne derse desin daima bağlanır.
            var isMandatory = responseProp.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);

            if (!isMandatory && !fields.Contains(responseProp.Name))
                continue;

            if (!entityProps.TryGetValue(responseProp.Name, out var entityProp))
                continue; // response'a özel/hesaplanan alan — entity'de karşılığı yok, atlanır

            if (!responseProp.PropertyType.IsAssignableFrom(entityProp.PropertyType))
                continue; // tip uyuşmazlığı — güvenli tarafta kal, atlanır

            bindings.Add(Expression.Bind(responseProp, Expression.Property(param, entityProp)));
        }

        // Id bile bağlanamadıysa (isimlendirme uyuşmuyor demektir) projeksiyon
        // güvenilir değil — null dönüp güvenli (eski) yola düşülsün.
        if (bindings.Count == 0)
            return null;

        var body = Expression.MemberInit(Expression.New(typeof(TResponse)), bindings);
        return Expression.Lambda<Func<TEntity, TResponse>>(body, param);
    }
}
