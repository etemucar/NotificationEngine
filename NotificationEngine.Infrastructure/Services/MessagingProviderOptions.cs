using Microsoft.Extensions.Configuration;

namespace NotificationEngine.Infrastructure.Service;

/// <summary>Amazon SES (e-posta). Gizli değerler user-secrets / ortam değişkeninden gelmeli; kimlik bilgisi verilmezse AWS varsayılan zinciri (ortam değişkeni, IAM rolü) kullanılır.</summary>
public sealed class SesOptions
{
    public string? Region { get; init; }              // örn. "eu-central-1"
    public string? FromAddress { get; init; }         // SES'te doğrulanmış gönderen adres
    public string? FromName { get; init; }
    public string? ConfigurationSet { get; init; }    // isteğe bağlı: bounce/şikayet olayları için
    public string BodyFormat { get; init; } = "Text"; // "Text" | "Html"
    public string? AccessKeyId { get; init; }
    public string? SecretAccessKey { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Region) && !string.IsNullOrWhiteSpace(FromAddress);

    public static SesOptions From(IConfiguration c)
    {
        const string p = "Notification:Email:Ses";
        return new SesOptions
        {
            Region = c[$"{p}:Region"],
            FromAddress = c[$"{p}:FromAddress"],
            FromName = c[$"{p}:FromName"],
            ConfigurationSet = c[$"{p}:ConfigurationSet"],
            BodyFormat = c[$"{p}:BodyFormat"] ?? "Text",
            AccessKeyId = c[$"{p}:AccessKeyId"],
            SecretAccessKey = c[$"{p}:SecretAccessKey"]
        };
    }
}

/// <summary>Firebase Cloud Messaging (push). Servis hesabı JSON'u user-secrets / ortam değişkeninden gelmeli.</summary>
/// <summary>Firebase Cloud Messaging (push). Servis hesabı JSON'u user-secrets / ortam değişkeninden gelmeli.</summary>
public sealed class FcmProviderOptions
{
    public string? CredentialsJson { get; init; }     // servis hesabı anahtarının JSON içeriği
    public string? CredentialsFile { get; init; }     // alternatif: JSON dosyasının yolu
    public string? ProjectId { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CredentialsJson) || !string.IsNullOrWhiteSpace(CredentialsFile);

    public static FcmProviderOptions From(IConfiguration c)
    {
        const string p = "Notification:Push:Fcm";
        return new FcmProviderOptions
        {
            CredentialsJson = c[$"{p}:CredentialsJson"],
            CredentialsFile = c[$"{p}:CredentialsFile"],
            ProjectId = c[$"{p}:ProjectId"]
        };
    }
}