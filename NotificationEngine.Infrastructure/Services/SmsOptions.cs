using Microsoft.Extensions.Configuration;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>Netgsm REST v2. Gizli değerler (UserCode, Password) user-secrets / ortam değişkeninden gelmeli, appsettings'e yazılmamalı.</summary>
public sealed class NetgsmOptions
{
    public string? UserCode { get; init; }
    public string? Password { get; init; }
    public string? MsgHeader { get; init; }          // Netgsm'de onaylı başlık
    public string MessageEncoding { get; init; } = "TR";
    public string? IysFilter { get; init; } = "0";   // "0" = bilgilendirme (OTP); ticari mesaj için farklı
    public string SendUrl { get; init; } = "https://api.netgsm.com.tr/sms/rest/v2/send";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserCode) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(MsgHeader);

    // IConfiguration indexer'ı ile okunur: ek paket (Binder) gerektirmez.
    public static NetgsmOptions From(IConfiguration c)
    {
        const string p = "Notification:Sms:Netgsm";
        return new NetgsmOptions
        {
            UserCode = c[$"{p}:UserCode"],
            Password = c[$"{p}:Password"],
            MsgHeader = c[$"{p}:MsgHeader"],
            MessageEncoding = c[$"{p}:Encoding"] ?? "TR",
            IysFilter = c[$"{p}:IysFilter"] ?? "0",
            SendUrl = c[$"{p}:SendUrl"] ?? "https://api.netgsm.com.tr/sms/rest/v2/send"
        };
    }
}

/// <summary>İleti Merkezi JSON API. Key/Secret user-secrets / ortam değişkeninden gelmeli.</summary>
public sealed class IletiMerkeziOptions
{
    public string? Key { get; init; }                // API anahtarı (public key)
    public string? Secret { get; init; }             // Gizli anahtar
    public string? Sender { get; init; }             // Onaylı başlık (en fazla 11 karakter)
    public string Iys { get; init; } = "0";          // "1" = İYS sorgulanır (ticari); OTP için "0"
    public string IysList { get; init; } = "BIREYSEL";
    public string SendUrl { get; init; } = "https://api.iletimerkezi.com/v1/send-sms/json";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Key) &&
        !string.IsNullOrWhiteSpace(Secret) &&
        !string.IsNullOrWhiteSpace(Sender);

    public static IletiMerkeziOptions From(IConfiguration c)
    {
        const string p = "Notification:Sms:IletiMerkezi";
        return new IletiMerkeziOptions
        {
            Key = c[$"{p}:Key"],
            Secret = c[$"{p}:Secret"],
            Sender = c[$"{p}:Sender"],
            Iys = c[$"{p}:Iys"] ?? "0",
            IysList = c[$"{p}:IysList"] ?? "BIREYSEL",
            SendUrl = c[$"{p}:SendUrl"] ?? "https://api.iletimerkezi.com/v1/send-sms/json"
        };
    }
}

internal static class SmsNumber
{
    /// <summary>"+905551112233" → "905551112233" (iki sağlayıcı da başında '+' olmayan biçimi bekler).</summary>
    public static string Normalize(string recipient) =>
        new string(recipient.Where(char.IsDigit).ToArray());
}