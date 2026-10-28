using Microsoft.Extensions.Configuration;

namespace NotificationEngine.Infrastructure;

/// <summary>SMTP e-posta. Kullanıcı adı ve şifre user-secrets / ortam değişkeninden gelmeli.</summary>
public sealed class SmtpOptions
{
    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string Security { get; init; } = "StartTls";   // "StartTls" | "SslOnConnect" | "Auto"
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FromAddress { get; init; }
    public string? FromName { get; init; }
    public string BodyFormat { get; init; } = "Text";     // "Text" | "Html"

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);

    public static SmtpOptions From(IConfiguration c)
    {
        const string p = "Notification:Email:Smtp";
        return new SmtpOptions
        {
            Host = c[$"{p}:Host"],
            Port = int.TryParse(c[$"{p}:Port"], out var port) ? port : 587,
            Security = c[$"{p}:Security"] ?? "StartTls",
            Username = c[$"{p}:Username"],
            Password = c[$"{p}:Password"],
            FromAddress = c[$"{p}:FromAddress"],
            FromName = c[$"{p}:FromName"],
            BodyFormat = c[$"{p}:BodyFormat"] ?? "Text"
        };
    }
}