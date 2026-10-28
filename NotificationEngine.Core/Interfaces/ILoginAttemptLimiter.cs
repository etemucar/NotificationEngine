namespace NotificationEngine.Core.Security;

/// <summary>
/// Şifre deneme sınırı: bir anahtar (kullanıcı tanımlayıcısı vb.) için art arda hatalı deneme
/// yapılırsa belirli bir süre yeni denemeye izin verilmez.
///
/// Anahtar kullanıcı VAR olsun olmasın aynı şekilde sayılır — aksi halde "kilitlendi" mesajı
/// hesabın varlığını sızdırırdı.
/// </summary>
public interface ILoginAttemptLimiter
{
    /// <summary>Anahtar şu an kilitliyse kalan süreyi, değilse null döner.</summary>
    TimeSpan? GetLockoutRemaining(string key);

    /// <summary>
    /// Hatalı deneme kaydeder. Bu deneme (ya da zaten süren) kilidi gerektiriyorsa kalan kilit süresini,
    /// değilse null döner.
    /// </summary>
    TimeSpan? RegisterFailure(string key);

    /// <summary>Başarılı girişten sonra sayacı sıfırlar.</summary>
    void Reset(string key);
}
