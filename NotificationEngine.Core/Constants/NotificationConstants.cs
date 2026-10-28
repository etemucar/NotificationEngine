namespace NotificationEngine.Core.Constants;

public static class NotificationConstants
{
    // Bölüm 11'de AÇIK karar — aşağıdaki değerler geçici varsayılanlardır.
    public const int MaxAttempts = 5;
    public const int BaseRetryDelaySeconds = 30;   // gecikme = Base * 2^(AttemptCount-1)
    public const int LockDurationSeconds = 120;    // Processing kilidi; dolarsa mesaj tekrar alınabilir
    public const int PollIntervalSeconds = 20;
    public const int ClaimBatchSize = 20;

    public const string DefaultCulture = "tr-TR";

    public const string ApiKeyHeader = "X-Api-Key";
    public const string ProviderConfigPrefix = "Notification:Providers"; // :Email | :Sms | :Push
    public const string ConsoleProviderName = "Console";
}