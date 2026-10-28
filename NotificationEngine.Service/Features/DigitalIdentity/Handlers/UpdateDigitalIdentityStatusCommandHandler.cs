using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public class UpdateDigitalIdentityStatusCommandHandler
    : IRequestHandler<UpdateDigitalIdentityStatusCommand, bool>
{
    private readonly IRepository<DigitalIdentity, Guid> _digitalIdentityRepository;
    private readonly IRepository<RefreshToken, int>      _refreshTokenRepository;
    private readonly IRepository<PartyRole, int>         _partyRoleRepository;
    private readonly ICurrentUserService                 _currentUserService;
    private readonly ILogger<UpdateDigitalIdentityStatusCommandHandler> _logger;

    public UpdateDigitalIdentityStatusCommandHandler(
        IRepository<DigitalIdentity, Guid> digitalIdentityRepository,
        IRepository<RefreshToken, int>     refreshTokenRepository,
        IRepository<PartyRole, int>        partyRoleRepository,
        ICurrentUserService                currentUserService,
        ILogger<UpdateDigitalIdentityStatusCommandHandler> logger)
    {
        _digitalIdentityRepository = digitalIdentityRepository;
        _refreshTokenRepository    = refreshTokenRepository;
        _partyRoleRepository       = partyRoleRepository;
        _currentUserService        = currentUserService;
        _logger                    = logger;
    }

    public async Task<bool> Handle(UpdateDigitalIdentityStatusCommand request, CancellationToken cancellationToken)
    {
        // 1. Yetki kontrolü — sadece SITE_ADMIN
        var partyRoleId = await _currentUserService.GetPartyRoleIdAsync(cancellationToken);
        var actingRole = await _partyRoleRepository.FindOneAsync(
            pr => pr.Id == partyRoleId,
            include: q => q.Include(pr => pr.PartyRoleType),
            ct: cancellationToken);

        if (actingRole?.PartyRoleType?.PartyRoleTypeCd != "SITE_ADMIN")
            throw new UnauthorizedException("Bu işlem için yetkiniz yok.");

        // 2. DigitalIdentity'yi bul
        var digitalIdentity = await _digitalIdentityRepository.FindOneAsync(
            d => d.Id == request.DigitalIdentityId,
            include: q => q.Include(d => d.ApplicationUser),
            asNoTracking: false,
            ct: cancellationToken);

        if (digitalIdentity is null)
            throw new NotFoundException("Kullanıcı bulunamadı.");

        if (digitalIdentity.Status == request.Status)
            return true;

        var previousStatus = digitalIdentity.Status;
        digitalIdentity.Status = request.Status;
        await _digitalIdentityRepository.UpdateAsync(digitalIdentity, cancellationToken);

        // 3. Active dışına çekiliyorsa: aktif refresh token'ları iptal et
        if (request.Status != GeneralStatus.Active && digitalIdentity.ApplicationUser is not null)
        {
            var activeTokens = await _refreshTokenRepository.FindAsync(
                t => t.ApplicationUserId == digitalIdentity.ApplicationUser.Id && !t.IsRevoked,
                orderBy: null,
                include: null,
                asNoTracking: false,
                ct: cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Kullanıcı statüsü değiştirildi. DigitalIdentityId: {Id}, {Previous} -> {New}",
            digitalIdentity.Id, previousStatus, request.Status);

        return true;
    }
}
