using System.Text.RegularExpressions;
using NotificationEngine.Core.Exceptions;   // namespace'i projene göre düzelt

namespace NotificationEngine.Service.Common;

/// <summary>{{parametre}} yer tutucularını doldurur. Eksik parametre → 400.</summary>
public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}")]
    private static partial Regex Placeholder();

    public static string Render(string template, IReadOnlyDictionary<string, string> parameters)
    {
        var missing = new List<string>();

        var result = Placeholder().Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            if (parameters.TryGetValue(key, out var value)) return value;
            missing.Add(key);
            return m.Value;
        });

        if (missing.Count > 0)
            throw new InvalidInputException(
                $"Şablon parametresi eksik: {string.Join(", ", missing.Distinct())}");

        return result;
    }
}