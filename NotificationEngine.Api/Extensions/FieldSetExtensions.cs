using System.Text.Json;
using System.Text.Json.Nodes;

namespace NotificationEngine.Api.Extensions;

/// <summary>
/// TMF Open API v5 "fields" (sparse fieldset) desteği. `fields` sorgu
/// parametresinde listelenmeyen alanlar response'tan tamamen çıkarılır
/// (null olarak DEĞİL — JSON'da hiç yer almaz). `id`, `href`, `@type` gibi
/// zorunlu zarf alanları `fields` ne içerirse içersin her zaman kalır.
/// </summary>
public static class FieldSetExtensions
{
    private static readonly string[] MandatoryEnvelopeFields = { "id", "href", "@type" };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// <paramref name="model"/>'i JSON'a çevirip, <paramref name="fields"/>'ta
    /// istenmeyen top-level alanları siler. <paramref name="fields"/> boş/null
    /// ise model olduğu gibi (tüm alanlarıyla) döner.
    /// </summary>
    public static JsonNode ApplyFieldSet<T>(this T model, string? fields)
    {
        var node = JsonSerializer.SerializeToNode(model, SerializerOptions)
                   ?? throw new InvalidOperationException($"{typeof(T).Name} serialize edilemedi.");

        if (string.IsNullOrWhiteSpace(fields))
            return node;

        var obj = node.AsObject();

        var requested = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToLowerInvariant())
            .Concat(MandatoryEnvelopeFields)
            .ToHashSet();

        // Not: TMF fields flat (top-level) çalışır — "detail.marketCap" gibi
        // nested alan seçimi kapsam dışı; "detail" istenirse tüm Detail objesi gelir.
        var keysToRemove = obj
            .Select(kv => kv.Key)
            .Where(key => !requested.Contains(key.ToLowerInvariant()))
            .ToList();

        foreach (var key in keysToRemove)
            obj.Remove(key);

        return obj;
    }
}
