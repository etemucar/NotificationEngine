namespace NotificationEngine.Core.Common;

/// <summary>
/// TMF "fields" (sparse fieldset) sorgu parametresinin entity'den bağımsız,
/// tek parse noktası. Herhangi bir domain'in (Instrument, Party, Question, ...)
/// handler'ı kendi FieldSet sınıfını yeniden yazmak yerine bunu kullanır —
/// bkz. FieldProjectionBuilder ile birlikte kullanım.
/// </summary>
public readonly struct FieldSet
{
    private readonly HashSet<string>? _fields;

    private FieldSet(HashSet<string>? fields) => _fields = fields;

    /// <summary>
    /// fields hiç belirtilmemiş (null/boş) — "tüm alanlar isteniyor" anlamına
    /// gelir. Bu durumda handler'lar sparse projeksiyon KURMAMALI, eski
    /// tam-yükleme yoluna düşmeli (bkz. FieldProjectionBuilder.Build).
    /// </summary>
    public bool IsAll => _fields is null;

    public static FieldSet Parse(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
            return new FieldSet(null);

        var set = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToLowerInvariant())
            .ToHashSet();

        return new FieldSet(set);
    }

    /// <summary>Bu tekil alan isteniyor mu (fields boşsa her zaman true).</summary>
    public bool Contains(string field) => IsAll || _fields!.Contains(field.ToLowerInvariant());

    /// <summary>
    /// Verilen alanlardan biri isteniyor mu — ya da fields hiç belirtilmemişse
    /// (IsAll) daima true. Pahalı bir Include/JOIN'in tetiklenip
    /// tetiklenmeyeceğine karar vermek için kullanılır
    /// (örn. fields.RequiresAny("detail")).
    /// </summary>
    public bool RequiresAny(params string[] fieldNames)
    {
        if (IsAll)
            return true;

        // struct içinde lambda, instance field'a (_fields) doğrudan "this"
        // üzerinden erişemiyor (CS1673) — local'e kopyalayıp onu kullanıyoruz.
        var fields = _fields!;
        return fieldNames.Any(f => fields.Contains(f.ToLowerInvariant()));
    }
}
