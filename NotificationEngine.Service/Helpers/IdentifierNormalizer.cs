namespace NotificationEngine.Service.Helpers;

/// <summary>
/// Kullanıcı tanımlayıcısını (email veya telefon) karşılaştırma/saklama için tek biçime getirir.
/// Email büyük/küçük harfe duyarsızdır (A@x.com ile a@x.com aynı hesaptır); telefon olduğu gibi kalır.
/// Kayıt, giriş ve deneme sınırı anahtarı HEP bu fonksiyondan geçmeli.
/// </summary>
public static class IdentifierNormalizer
{
    public static string Normalize(string? identifier)
    {
        var trimmed = (identifier ?? string.Empty).Trim();
        return trimmed.Contains('@') ? trimmed.ToLowerInvariant() : trimmed;
    }
}
