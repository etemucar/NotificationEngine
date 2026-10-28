using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace NotificationEngine.Api.Services;

public readonly record struct RateLimitDecision(bool Allowed, TimeSpan RetryAfter);

/// <summary>Anında gönderim (OTP/sıfırlama) ucunda aynı alıcıya art arda mesaj atılmasını sınırlar.</summary>
public interface ISendNowRateLimiter
{
    RateLimitDecision Check(int organizationId, string recipient);
}

/// <summary>
/// (kuruluş, alıcı) başına sabit pencere sayacı, bellekte tutulur. Varsayılan: 10 dakikada en fazla 3 mesaj.
/// Alıcı kimliği: e-posta için küçük harfli adres, telefon için yalnızca rakamlar.
/// Ayarlar: Notification:SendNow:RecipientMaxPerWindow, Notification:SendNow:RecipientWindowMinutes.
/// DİKKAT: sayaç uygulama örneğine özeldir; birden çok örnek çalışırsa limit örnek sayısıyla çarpılır
/// (o zaman Redis gibi ortak bir depoya taşınmalı).
/// </summary>
public sealed class SendNowRateLimiter : ISendNowRateLimiter
{
    private sealed class Counter
    {
        public int Count;
        public DateTimeOffset WindowEnd;
    }

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _time;
    private readonly int _maxPerWindow;
    private readonly TimeSpan _window;

    public SendNowRateLimiter(IMemoryCache cache, TimeProvider time, IConfiguration configuration)
    {
        _cache = cache;
        _time = time;
        _maxPerWindow = int.TryParse(configuration["Notification:SendNow:RecipientMaxPerWindow"], out var max) && max > 0
            ? max : 3;
        _window = TimeSpan.FromMinutes(
            int.TryParse(configuration["Notification:SendNow:RecipientWindowMinutes"], out var minutes) && minutes > 0
                ? minutes : 10);
    }

    public RateLimitDecision Check(int organizationId, string recipient)
    {
        var now = _time.GetUtcNow();

        // Alıcı önbellek anahtarında düz yazılmaz. E-postada rakam olmayabilir, bu yüzden adresin kendisi
        // (küçük harfli) kullanılır; telefonda biçim farklarını (+90, boşluk, tire) yok etmek için yalnızca rakamlar.
        var identity = recipient.Contains('@')
            ? recipient.Trim().ToLowerInvariant()
            : new string(recipient.Where(char.IsDigit).ToArray());
        if (identity.Length == 0)
            identity = recipient.Trim();

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..16];
        var key = $"sendnow:{organizationId}:{hash}";

        var counter = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _window;
            return new Counter { WindowEnd = now + _window };
        })!;

        lock (counter)
        {
            if (counter.Count >= _maxPerWindow)
                return new RateLimitDecision(false, counter.WindowEnd - now);

            counter.Count++;
            return new RateLimitDecision(true, TimeSpan.Zero);
        }
    }
}