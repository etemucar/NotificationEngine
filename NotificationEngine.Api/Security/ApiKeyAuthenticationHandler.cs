using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Security;

namespace NotificationEngine.Api.Security;

public static class ApiKeyAuthConstants
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string OrganizationIdClaim = "organization_id";
}

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// SchedulerEngine, FinYo, Censitor gibi dış servislerin API'sine erişimini "X-Api-Key"
/// header'ıyla doğrular. Credential/CredentialCharacteristic'te
/// (CredentialType.ApiKey, Characteristic Name="apiKeyHash") saklanan
/// hash'lerle karşılaştırır - appsettings'e hiçbir şey yazılmaz, yeni bir
/// dış servis eklemek sadece veri eklemekle olur (kod/appsettings değişmez).
///
/// Az sayıda servis credential'ı olacağı varsayımıyla (şu an NotificationEngine, DocDes),
/// hepsini IMemoryCache'te tutup her istekte DB'ye gitmiyoruz.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly IRepository<Credential, Guid> _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMemoryCache _cache;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IRepository<Credential, Guid> credentialRepository,
        IPasswordHasher passwordHasher,
        IMemoryCache cache)
        : base(options, logger, encoder)
    {
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthConstants.HeaderName, out var providedKeyValues))
        {
            return AuthenticateResult.Fail($"{ApiKeyAuthConstants.HeaderName} header eksik.");
        }

        var providedKey = providedKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.Fail($"{ApiKeyAuthConstants.HeaderName} boş olamaz.");
        }

        var candidates = await GetCachedApiKeyCredentialsAsync();

        // IPasswordHasher.Hash muhtemelen non-deterministic (her seferinde
        // farklı hash üretiyor - salt'lı), bu yüzden gelen key'i hash'leyip
        // eşitlik araması YAPAMAYIZ. Bunun yerine, az sayıda aday olduğu için
        // her birine karşı Verify() çağırıyoruz - ilk eşleşen kazanır.
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate.HashedKey) || candidate.OrganizationId <= 0)
                continue; // kuruluşa bağlanamayan anahtar kabul edilmez

            if (_passwordHasher.Verify(providedKey, candidate.HashedKey))
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, candidate.ServiceName),
                    new Claim("client_id", candidate.ServiceName),
                    new Claim("credential_id", candidate.CredentialId.ToString()),
                    new Claim(ApiKeyAuthConstants.OrganizationIdClaim, candidate.OrganizationId.ToString())
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
        }

        return AuthenticateResult.Fail("Geçersiz API Key.");
    }

    private async Task<IReadOnlyList<ApiKeyCacheEntry>> GetCachedApiKeyCredentialsAsync()
    {
        if (_cache.TryGetValue(ApiKeyCacheConstants.CacheKey, out IReadOnlyList<ApiKeyCacheEntry>? cached) && cached is not null)
        {
            return cached;
        }

        // DigitalIdentity -> PartyRole -> Party -> Organization zincirinden
        // servis adını (Organization.Name) çekiyoruz. Bu navigation zinciri
        // sizin gerçek PartyRole/Party modelinize göre farklıysa (örn.
        // PartyRole'de Party navigation'ı yoksa) bu satırı düzeltmen gerekir.
        //
        // ÖNEMLİ: DigitalIdentity.Status == Active filtresi olmadan, bir
        // servisi UpdateDigitalIdentityStatusCommand ile Suspended/Inactive
        // yapmak API key'ini GEÇERSİZ KILMAZ - bu filtre olmadan iptal
        // mekanizması işe yaramaz.
        var entries = await _credentialRepository.FindSelectAsync(
            predicate: c => c.CredentialType == CredentialType.ApiKey
                && c.DigitalIdentity.Status == GeneralStatus.Active,
            selector: c => new ApiKeyCacheEntry
            {
                CredentialId = c.Id,
                // Servis (FinYo, Censitor) register-organization ile kaydedilir: Party'si doğrudan Organization'dır.
                OrganizationId = c.DigitalIdentity.PartyRole.Party.Organization != null
                    ? c.DigitalIdentity.PartyRole.Party.Organization.Id
                    : 0,
                HashedKey = c.Characteristics
                    .Where(ch => ch.Name == "apiKeyHash")
                    .Select(ch => ch.Value)
                    .FirstOrDefault() ?? string.Empty,
                ServiceName = c.DigitalIdentity.PartyRole.Party.Organization != null
                    ? c.DigitalIdentity.PartyRole.Party.Organization!.Name
                    : "Bilinmeyen Servis"
            });

        _cache.Set(ApiKeyCacheConstants.CacheKey, entries, ApiKeyCacheConstants.CacheDuration);

        return entries;
    }

    private class ApiKeyCacheEntry
    {
        public Guid CredentialId { get; set; }
        public int OrganizationId { get; set; }
        public string HashedKey { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
    }
}