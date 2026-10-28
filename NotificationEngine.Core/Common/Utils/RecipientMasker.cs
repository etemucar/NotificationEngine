namespace NotificationEngine.Core.Common;

/// <summary>Loglara alıcı yazılırken kullanılır; ham e-posta/telefon/token asla loglanmaz.</summary>
public static class RecipientMasker
{
    public static string Mask(string? recipient)
    {
        if (string.IsNullOrEmpty(recipient)) return "***";

        var at = recipient.IndexOf('@');
        if (at > 0)
            return recipient[0] + "***" + recipient[at..];

        return recipient.Length <= 4
            ? "***"
            : recipient[..2] + "***" + recipient[^2..];
    }
}