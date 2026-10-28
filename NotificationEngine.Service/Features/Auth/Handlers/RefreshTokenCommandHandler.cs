using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Helpers;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Common.References;

namespace NotificationEngine.Service.Features.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IRepository<RefreshToken, int>    _refreshTokenRepository;
    private readonly IRepository<ApplicationUser, int> _userRepository;
    private readonly ITokenService                     _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRepository<RefreshToken, int>    refreshTokenRepository,
        IRepository<ApplicationUser, int> userRepository,
        ITokenService                     tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository         = userRepository;
        _tokenService           = tokenService;
        _logger                 = logger;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Token'ı bul
        var existingToken = await _refreshTokenRepository.FindOneAsync(
            t => t.Token == request.RefreshToken,
            i => i.Include(t => t.ApplicationUser)
                    .ThenInclude(u => u.DigitalIdentity)
                        .ThenInclude(d => d.PartyRole)
                            .ThenInclude(pr => pr.Party)
                                .ThenInclude(p => p.Individual)
                  .Include(t => t.ApplicationUser)
                    .ThenInclude(u => u.DigitalIdentity)
                        .ThenInclude(d => d.Credentials)
                            .ThenInclude(c => c.ContactMedia),
            asNoTracking: false,
            ct: cancellationToken);

        if (existingToken is null)
            throw new UnauthorizedException("Refresh token bulunamadı.");

        // 2. Aktif mi?
        if (!existingToken.IsActive)
            throw new UnauthorizedException("Refresh token geçersiz veya süresi dolmuş.");

        // 3. Eski token'ı iptal et
        existingToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        // 4. Kullanıcı bilgilerini çöz — LoginCommandHandler/RegisterCommandHandler/
        // CreateAdminUserCommandHandler ile AYNI paylaşılan helper kullanılıyor.
        // Önceden bu mantık burada elle (Helper'dan bağımsız) tekrar yazılmıştı —
        // ikisi arasında drift riski vardı (bkz. review notu). Artık tek kaynak.
        var appUser = existingToken.ApplicationUser;
        var (userId, userName, userIdentifier) = Helper.ResolveUserInfo(appUser);

        var partyRole = appUser.DigitalIdentity.PartyRole;
        var roleCd = ReferenceData.PartyRoleType.ToCode(partyRole.PartyRoleTypeId);

        // 5. Yeni token'ları üret
        var accessToken  = _tokenService.CreateAccessToken(userName, userId, userIdentifier, roleCd);
        var refreshToken = _tokenService.CreateRefreshToken(userId);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token yenilendi. UserId: {UserId}", userId);

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
}
