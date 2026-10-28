namespace NotificationEngine.Core.Enums;

public enum CredentialType
{
    Password,  // Local auth
    LDAP,
    AzureAD,
    Token,
    Biometric,
    ApiKey,         // Dış servisin BİZE gönderdiği anahtar (hash'lenir, doğrulama amaçlı)
    OutboundApiKey  // BİZİM dış servise giderken göndereceğimiz anahtar (şifrelenir, geri okunabilir olmalı)
}