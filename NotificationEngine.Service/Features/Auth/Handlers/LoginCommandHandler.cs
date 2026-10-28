using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Helpers;
using NotificationEngine.Core.Common.References;

namespace NotificationEngine.Service.Features.Handlers;

/// <summary>
/// DENEME SINIRI: tanımlayıcı başına art arda hatalı girişte (bkz. LoginAttemptPolicy) bir süre
/// giriş denenemez. Kilit, kullanıcı VAR olsun olmasın aynı şekilde uygulanır; kilitliyken doğru
/// şifre bile kabul edilmez (şifre doğruluğu sızmasın). "Hesap aktif değil" sayılmaz.
///
/// EMAIL: tanımlayıcı IdentifierNormalizer ile normalize edilir ve DB'deki email ile lower() üzerinden
/// karşılaştırılır — eski (karışık harfli) kayıtlar da bulunur.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IRepository<ApplicationUser, int> _userRepository;
    private readonly IRepository<RefreshToken, int>    _refreshTokenRepository;
    private readonly ITokenService                     _tokenService;
    private readonly IPasswordHasher                   _passwordHasher;
    private readonly ILoginAttemptLimiter              _attemptLimiter;
    private readonly ILogger<LoginCommandHandler>      _logger;

    public LoginCommandHandler(
        IRepository<ApplicationUser, int> userRepository,
        IRepository<RefreshToken, int>    refreshTokenRepository,
        ITokenService                     tokenService,
        IPasswordHasher                   passwordHasher,
        ILoginAttemptLimiter              attemptLimiter,
        ILogger<LoginCommandHandler>      logger)
    {
        _userRepository         = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService           = tokenService;
        _passwordHasher         = passwordHasher;
        _attemptLimiter         = attemptLimiter;
        _logger                 = logger;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identifier = IdentifierNormalizer.Normalize(request.Identifier);
        var limiterKey = $"login:{identifier}";

        // 0. Kilitli mi? (kullanıcı var olsun olmasın aynı davranış)
        var lockedFor = _attemptLimiter.GetLockoutRemaining(limiterKey);
        if (lockedFor is not null)
            throw new UnauthorizedException(LoginAttemptPolicy.LockoutMessage(lockedFor.Value));

        // 1. Identifier ile kullanıcıyı bul (email veya telefon)
        var user = await _userRepository.FindOneAsync(
            u => u.DigitalIdentity.Credentials
                .Any(c => c.ContactMedia
                    .Any(cm => (cm.Email != null && cm.Email.ToLower() == identifier)
                            || cm.PhoneNumber == identifier)),
            i => i
                .Include(u => u.DigitalIdentity)
                    .ThenInclude(d => d.Credentials)
                        .ThenInclude(c => c.ContactMedia)
                .Include(u => u.DigitalIdentity)
                    .ThenInclude(d => d.Credentials)
                        .ThenInclude(c => c.Characteristics)
                .Include(u => u.DigitalIdentity)
                    .ThenInclude(u => u.PartyRole)
                        .ThenInclude(pr => pr.Party)
                            .ThenInclude(p => p.Individual),
            asNoTracking: false,
            ct: cancellationToken);

        if (user is null)
            throw FailedAttempt(limiterKey, identifier, "Kullanıcı bulunamadı.");

        // 2. DigitalIdentity status kontrolü
        if (user.DigitalIdentity.Status != GeneralStatus.Active)
            throw new UnauthorizedException("Hesap aktif değil.");

        // 3. Şifre doğrula
        var passwordCredential = user.DigitalIdentity.Credentials
            .FirstOrDefault(c => c.CredentialType == CredentialType.Password);

        if (passwordCredential is null)
            throw FailedAttempt(limiterKey, identifier, "Geçersiz kimlik bilgisi.");

        var passwordHash = passwordCredential.Characteristics
            .FirstOrDefault(ch => ch.Name == "passwordHash")?.Value;

        if (!_passwordHasher.Verify(request.Password, passwordHash))
            throw FailedAttempt(limiterKey, identifier, "Kullanıcı adı veya şifre hatalı.");

        // Başarılı giriş: sayaç sıfırlanır
        _attemptLimiter.Reset(limiterKey);

        // 4. Kullanıcı bilgilerini hazırla
        var (userId, userName, userIdentifier) = Helper.ResolveUserInfo(user);
        var roleCd = ReferenceData.PartyRoleType.ToCode(user.DigitalIdentity.PartyRole.PartyRoleTypeId);

        // 5. Access token üret
        var accessToken = _tokenService.CreateAccessToken(userName, user.Id, userIdentifier, roleCd);

        // 6. Refresh token üret ve kaydet
        var refreshToken = _tokenService.CreateRefreshToken(user.Id);
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            ApplicationUserId = user.Id,
            Token             = refreshToken.Token,
            ExpiresAt         = refreshToken.ExpiresAt,
            CreatedAt         = DateTime.UtcNow,
            CreatedByIp       = null
        }, cancellationToken);

        _logger.LogInformation(
            "Kullanıcı giriş yaptı. UserId: {UserId}, Identifier: {Identifier}",
            user.Id, identifier);

        return new AuthResult
        {
            UserId                 = userId,
            UserName               = userName,
            UserIdentifier         = userIdentifier,
            AccessToken            = accessToken.Token,
            AccessTokenExpiration  = accessToken.Expiration,
            RefreshToken           = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.ExpiresAt
        };
    }

    /// <summary>
    /// Hatalı denemeyi sayar. Bu deneme kilidi tetiklediyse kullanıcıya kilit mesajı, değilse asıl
    /// hata mesajı döner. Fırlatma çağıranın tarafında (throw FailedAttempt(...)) — derleyici akışı doğru görsün.
    /// </summary>
    private UnauthorizedException FailedAttempt(string limiterKey, string identifier, string message)
    {
        var lockout = _attemptLimiter.RegisterFailure(limiterKey);
        if (lockout is null)
            return new UnauthorizedException(message);

        _logger.LogWarning(
            "Hatalı giriş denemesi sınırı aşıldı, giriş geçici olarak kilitlendi. Identifier: {Identifier}",
            identifier);

        return new UnauthorizedException(LoginAttemptPolicy.LockoutMessage(lockout.Value));
    }
}
