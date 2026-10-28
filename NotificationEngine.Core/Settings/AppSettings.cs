using NotificationEngine.Core.Security;

namespace NotificationEngine.Settings.Core;

public class AppSettings
{
    public string SecretKey { get; set; }= string.Empty;
    public string EncryptionKey { get; set; }= string.Empty;
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public LoggingSettings LoggingSettings { get; set; } = new();
    public List<string> AllowedHosts { get; set; } = new();
    public TokenOptions TokenOptions { get; set; } = new();
}

public class LoggingSettings
{
    public bool IncludeScopes { get; set; } = false;
    public Loglevel LogLevel { get; set; } = new();
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public class Loglevel
{
    public string Default { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Microsoft { get; set; } = string.Empty;
}
