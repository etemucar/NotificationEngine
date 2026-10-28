using MediatR;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Exceptions;

namespace NotificationEngine.Service.Features.Handlers;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IRepository<RefreshToken, int> _refreshTokenRepository;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        IRepository<RefreshToken, int>      refreshTokenRepository,
        ILogger<RevokeTokenCommandHandler>  logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger                 = logger;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Token'ı bul
        var token = await _refreshTokenRepository.FindOneAsync(
            t => t.Token == request.RefreshToken,
            asNoTracking: false,
            ct: cancellationToken);

        if (token is null)
            throw new UnauthorizedException("Refresh token bulunamadı.");

        // 2. Zaten revoked mu?
        if (token.IsRevoked)
            return true;

        // 3. Revoke et
        token.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        _logger.LogInformation(
            "Refresh token iptal edildi. TokenId: {TokenId}", token.Id);

        return true;
    }
}
