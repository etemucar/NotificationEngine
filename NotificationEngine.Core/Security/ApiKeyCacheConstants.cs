namespace NotificationEngine.Core.Security;

/// <summary>
/// ApiKey credential cache'i için paylaşılan sabitler. Core'da tanımlı
/// çünkü hem Api (ApiKeyAuthenticationHandler - cache'i okuyor) hem Service
/// (RegisterOrganizationCommandHandler - yeni credential eklenince cache'i
/// geçersiz kılıyor) buna ihtiyaç duyuyor; Service'in Api'ye bağımlı olması
/// katman yönünü tersine çevirir, bu yüzden ortak nokta Core.
/// </summary>
public static class ApiKeyCacheConstants
{
    public const string CacheKey = "ExternalServiceApiKeyCredentials";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
}