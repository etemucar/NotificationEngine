using NotificationEngine.Core.Security;
using Microsoft.Extensions.Caching.Memory;

namespace NotificationEngine.Service.Common;

/// <summary>Deneme sınırı politikası — değerleri tek yerden değiştirmek için.</summary>
public static class LoginAttemptPolicy
{
    /// <summary>Bu kadar hatalı denemede kilit devreye girer.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Hatalı denemelerin sayıldığı pencere (ilk hatadan itibaren).</summary>
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);

    /// <summary>Kilit süresi.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static string LockoutMessage(TimeSpan remaining)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        return $"Çok fazla hatalı deneme yapıldı. Lütfen {minutes} dakika sonra tekrar deneyin.";
    }
}

/// <summary>
/// ILoginAttemptLimiter'ın IMemoryCache tabanlı implementasyonu.
///
/// IMemoryCache DOĞRUDAN kullanılıyor — DispatchSchedulerWebhookCommandHandler'daki emsalle aynı
/// gerekçe: framework'ün genel bir cross-cutting abstraction'ı, Infrastructure'a özel bir servis değil.
///
/// TEK INSTANCE VARSAYIMI: sayaçlar bu process'in belleğinde. (1) Uygulama yeniden başlayınca
/// sıfırlanır. (2) Birden fazla instance çalışırsa her instance kendi sayacını tutar, saldırgan
/// instance sayısı kadar fazla deneme yapabilir — o noktada paylaşımlı bir depoya (Redis/DB) geçilmeli.
///
/// BİLİNEN TAKAS: anahtar kullanıcı tanımlayıcısı olduğu için, başkasının hesabını bilerek yanlış
/// şifreyle kilitlemek mümkündür (hizmet engelleme). IP bazlı hız sınırı bunu azaltır (ayrıca ele alınacak).
/// </summary>
public class LoginAttemptLimiter : ILoginAttemptLimiter
{
    private readonly IMemoryCache _cache;

    private sealed class State
    {
        public readonly object Gate = new();
        public int Failures;
        public DateTime FirstFailureAt;
        public DateTime? LockedUntil;
    }

    public LoginAttemptLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public TimeSpan? GetLockoutRemaining(string key)
    {
        if (!_cache.TryGetValue(key, out State? state) || state is null)
            return null;

        lock (state.Gate)
            return Remaining(state, DateTime.UtcNow);
    }

    public TimeSpan? RegisterFailure(string key)
    {
        var state = _cache.GetOrCreate(key, entry =>
        {
            // Pencere + kilit süresi kadar yaşar: kilit pencerenin sonunda devreye girse bile süre dolana kadar kaybolmaz.
            entry.AbsoluteExpirationRelativeToNow = LoginAttemptPolicy.FailureWindow + LoginAttemptPolicy.LockoutDuration;
            return new State();
        })!;

        var now = DateTime.UtcNow;

        lock (state.Gate)
        {
            var remaining = Remaining(state, now);
            if (remaining is not null)
                return remaining; // zaten kilitli

            if (state.Failures == 0 || now - state.FirstFailureAt > LoginAttemptPolicy.FailureWindow)
            {
                state.Failures = 0;
                state.FirstFailureAt = now;
            }

            state.Failures++;

            if (state.Failures < LoginAttemptPolicy.MaxFailedAttempts)
                return null;

            state.LockedUntil = now + LoginAttemptPolicy.LockoutDuration;
            state.Failures = 0; // kilit bitince temiz pencere
            return LoginAttemptPolicy.LockoutDuration;
        }
    }

    public void Reset(string key) => _cache.Remove(key);

    private static TimeSpan? Remaining(State state, DateTime now) =>
        state.LockedUntil is { } until && until > now ? until - now : null;
}
