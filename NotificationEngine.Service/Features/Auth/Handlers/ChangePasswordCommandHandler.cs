using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Security;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

/// <summary>
/// Şifre değişikliği + tüm refresh token'ların iptali TEK SaveChanges ile yazılır
/// (tek DB transaction'ı): ya ikisi birden olur ya hiçbiri. Böylece "şifre değişti ama eski
/// oturumlar açık kaldı" ara durumu oluşmaz.
///
/// DENEME SINIRI: mevcut şifrenin tahmin edilmesine karşı (oturumu çalınmış biri) kullanıcı başına
/// ayrı bir sayaç (login sayacından bağımsız anahtar) tutulur. Kilit mesajı da 400 (InvalidInputException)
/// döner — 401 istemcide "oturum süresi doldu" akışını tetikleyebilir.
///
/// Yanlış mevcut şifre 401 değil 400 (InvalidInputException) döner: kullanıcının oturumu
/// geçerli, hatalı olan girdisi.
///
/// SINIR: access token (JWT) sunucuda iptal edilemez; süresi dolana kadar geçerli kalır.
/// Yalnızca refresh token'lar iptal edildiği için yeni access token alınamaz.
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IRepository<ApplicationUser, int>          _userRepository;
    private readonly IRepository<CredentialCharacteristic, int> _characteristicRepository;
    private readonly IRepository<RefreshToken, int>             _refreshTokenRepository;
    private readonly IPasswordHasher                            _passwordHasher;
    private readonly ILoginAttemptLimiter                       _attemptLimiter;
    private readonly IUnitOfWork                                _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler>      _logger;

    public ChangePasswordCommandHandler(
        IRepository<ApplicationUser, int>          userRepository,
        IRepository<CredentialCharacteristic, int> characteristicRepository,
        IRepository<RefreshToken, int>             refreshTokenRepository,
        IPasswordHasher                            passwordHasher,
        ILoginAttemptLimiter                       attemptLimiter,
        IUnitOfWork                                unitOfWork,
        ILogger<ChangePasswordCommandHandler>      logger)
    {
        _userRepository           = userRepository;
        _characteristicRepository = characteristicRepository;
        _refreshTokenRepository   = refreshTokenRepository;
        _passwordHasher           = passwordHasher;
        _attemptLimiter           = attemptLimiter;
        _unitOfWork               = unitOfWork;
        _logger                   = logger;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var limiterKey = $"change-password:{request.UserId}";

        // 0. Kilitli mi?
        var lockedFor = _attemptLimiter.GetLockoutRemaining(limiterKey);
        if (lockedFor is not null)
            throw new InvalidInputException(LoginAttemptPolicy.LockoutMessage(lockedFor.Value));

        // 1. Kullanıcıyı şifre credential'ıyla birlikte (takip edilen) yükle
        var user = await _userRepository.FindOneAsync(
            u => u.Id == request.UserId,
            i => i
                .Include(u => u.DigitalIdentity)
                    .ThenInclude(d => d.Credentials)
                        .ThenInclude(c => c.Characteristics),
            asNoTracking: false,
            ct: cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Kullanıcı bulunamadı.");

        if (user.DigitalIdentity.Status != GeneralStatus.Active)
            throw new UnauthorizedException("Hesap aktif değil.");

        // 2. Mevcut şifreyi doğrula
        var passwordCredential = user.DigitalIdentity.Credentials
            .FirstOrDefault(c => c.CredentialType == CredentialType.Password)
            ?? throw new UnauthorizedException("Geçersiz kimlik bilgisi.");

        var hashCharacteristic = passwordCredential.Characteristics
            .FirstOrDefault(ch => ch.Name == "passwordHash")
            ?? throw new UnauthorizedException("Geçersiz kimlik bilgisi.");

        if (!_passwordHasher.Verify(request.CurrentPassword, hashCharacteristic.Value))
        {
            var lockout = _attemptLimiter.RegisterFailure(limiterKey);

            if (lockout is not null)
            {
                _logger.LogWarning(
                    "Şifre değiştirme için hatalı deneme sınırı aşıldı, geçici olarak kilitlendi. UserId: {UserId}",
                    user.Id);
                throw new InvalidInputException(LoginAttemptPolicy.LockoutMessage(lockout.Value));
            }

            throw new InvalidInputException("Mevcut şifre hatalı.");
        }

        // 3. Yeni şifreyi yaz
        hashCharacteristic.Value = _passwordHasher.Hash(request.NewPassword);
        await _characteristicRepository.UpdateAsync(hashCharacteristic, cancellationToken);

        // 4. Bu kullanıcının aktif TÜM refresh token'larını iptal et (bu cihaz dahil)
        var activeTokens = await _refreshTokenRepository.FindAsync(
            t => t.ApplicationUserId == user.Id && !t.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var token in activeTokens)
            token.IsRevoked = true;

        if (activeTokens.Count > 0)
            await _refreshTokenRepository.UpdateRangeAsync(activeTokens, cancellationToken);

        // 5. Tek SaveChanges: şifre + token iptalleri birlikte
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _attemptLimiter.Reset(limiterKey);

        _logger.LogInformation(
            "Kullanıcı şifresini değiştirdi, {RevokedCount} refresh token iptal edildi. UserId: {UserId}",
            activeTokens.Count, user.Id);
    }
}
