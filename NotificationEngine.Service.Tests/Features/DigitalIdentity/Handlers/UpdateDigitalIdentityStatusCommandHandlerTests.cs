using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class UpdateDigitalIdentityStatusCommandHandlerTests
{
    private readonly Mock<IRepository<DigitalIdentity, Guid>> _digitalIdentityRepositoryMock;
    private readonly Mock<IRepository<RefreshToken, int>>     _refreshTokenRepositoryMock;
    private readonly Mock<IRepository<PartyRole, int>>        _partyRoleRepositoryMock;
    private readonly Mock<ICurrentUserService>                _currentUserServiceMock;
    private readonly Mock<ILogger<UpdateDigitalIdentityStatusCommandHandler>> _loggerMock;
    private readonly UpdateDigitalIdentityStatusCommandHandler _handler;

    private const int ActingPartyRoleId = 99; // yetki kontrolünü yapan (çağıran) kullanıcının PartyRoleId'si
    private static readonly Guid DigitalIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UpdateDigitalIdentityStatusCommandHandlerTests()
    {
        _digitalIdentityRepositoryMock = new Mock<IRepository<DigitalIdentity, Guid>>();
        _refreshTokenRepositoryMock    = new Mock<IRepository<RefreshToken, int>>();
        _partyRoleRepositoryMock       = new Mock<IRepository<PartyRole, int>>();
        _currentUserServiceMock        = new Mock<ICurrentUserService>();
        _loggerMock                    = new Mock<ILogger<UpdateDigitalIdentityStatusCommandHandler>>();

        _handler = new UpdateDigitalIdentityStatusCommandHandler(
            _digitalIdentityRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _partyRoleRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void SetupActingUser(int actingPartyRoleId = ActingPartyRoleId)
    {
        _currentUserServiceMock
            .Setup(x => x.GetPartyRoleIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(actingPartyRoleId);
    }

    private void SetupPartyRoleRepository(bool actingIsSiteAdmin = true)
    {
        var actingRole = new PartyRole
        {
            Id = ActingPartyRoleId,
            PartyRoleType = new PartyRoleType
            {
                PartyRoleTypeCd = actingIsSiteAdmin ? "SITE_ADMIN" : "USER"
            }
        };

        _partyRoleRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<PartyRole, bool>>>(),
                It.IsAny<Func<IQueryable<PartyRole>, IQueryable<PartyRole>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PartyRole, bool>> predicate,
                           Func<IQueryable<PartyRole>, IQueryable<PartyRole>>? include,
                           bool asNoTracking,
                           CancellationToken ct) =>
            {
                var compiled = predicate.Compile();
                return compiled(actingRole) ? actingRole : null;
            });
    }

    private void SetupDigitalIdentity(
        GeneralStatus currentStatus = GeneralStatus.Active,
        ApplicationUser? applicationUser = null)
    {
        var digitalIdentity = new DigitalIdentity
        {
            Id              = DigitalIdentityId,
            Status          = currentStatus,
            ApplicationUser = applicationUser
        };

        _digitalIdentityRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<DigitalIdentity, bool>>>(),
                It.IsAny<Func<IQueryable<DigitalIdentity>, IQueryable<DigitalIdentity>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<DigitalIdentity, bool>> predicate,
                           Func<IQueryable<DigitalIdentity>, IQueryable<DigitalIdentity>>? include,
                           bool asNoTracking,
                           CancellationToken ct) =>
            {
                var compiled = predicate.Compile();
                return compiled(digitalIdentity) ? digitalIdentity : null;
            });
    }

    private static UpdateDigitalIdentityStatusCommand BuildCommand(
        GeneralStatus status,
        Guid? digitalIdentityId = null)
    {
        // UpdateDigitalIdentityStatusCommand pozisyonel bir record — object initializer değil,
        // constructor argümanlarıyla oluşturuluyor.
        return new UpdateDigitalIdentityStatusCommand(
            digitalIdentityId ?? DigitalIdentityId,
            status);
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ShouldChangeStatusAndReturnTrue()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();
        SetupDigitalIdentity(currentStatus: GeneralStatus.Active);

        // Act
        var result = await _handler.Handle(BuildCommand(GeneralStatus.Passive), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _digitalIdentityRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<DigitalIdentity>(d => d.Status == GeneralStatus.Passive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActingUserNotSiteAdmin_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository(actingIsSiteAdmin: false);

        // Act
        var act = async () => await _handler.Handle(BuildCommand(GeneralStatus.Passive), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*yetkiniz yok*");

        _digitalIdentityRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DigitalIdentityNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();

        _digitalIdentityRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<DigitalIdentity, bool>>>(),
                It.IsAny<Func<IQueryable<DigitalIdentity>, IQueryable<DigitalIdentity>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DigitalIdentity?)null);

        // Act
        var act = async () => await _handler.Handle(BuildCommand(GeneralStatus.Passive), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SameStatus_ShouldReturnTrueWithoutUpdatingOrRevoking()
    {
        // Arrange — istenen status mevcut statüyle aynı: erken çıkış (no-op) beklenir
        SetupActingUser();
        SetupPartyRoleRepository();
        SetupDigitalIdentity(currentStatus: GeneralStatus.Active);

        // Act
        var result = await _handler.Handle(BuildCommand(GeneralStatus.Active), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _digitalIdentityRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _refreshTokenRepositoryMock.Verify(
            x => x.FindAsync(
                It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StatusMovedAwayFromActive_ShouldRevokeAllActiveRefreshTokens()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();

        var applicationUser = new ApplicationUser { Id = 5 };
        SetupDigitalIdentity(currentStatus: GeneralStatus.Active, applicationUser: applicationUser);

        var activeToken1 = new RefreshToken { Id = 1, ApplicationUserId = 5, IsRevoked = false };
        var activeToken2 = new RefreshToken { Id = 2, ApplicationUserId = 5, IsRevoked = false };

        _refreshTokenRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { activeToken1, activeToken2 });

        // Act
        await _handler.Handle(BuildCommand(GeneralStatus.Passive), TestContext.Current.CancellationToken);

        // Assert
        activeToken1.IsRevoked.Should().BeTrue();
        activeToken2.IsRevoked.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_StatusMovedBackToActive_ShouldNotTouchRefreshTokens()
    {
        // Arrange — status Active'e dönüyor, revoke akışı tetiklenmemeli
        SetupActingUser();
        SetupPartyRoleRepository();

        var applicationUser = new ApplicationUser { Id = 5 };
        SetupDigitalIdentity(currentStatus: GeneralStatus.Passive, applicationUser: applicationUser);

        // Act
        await _handler.Handle(BuildCommand(GeneralStatus.Active), TestContext.Current.CancellationToken);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.FindAsync(
                It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NoApplicationUser_ShouldNotThrowAndShouldNotTouchRefreshTokens()
    {
        // Arrange — DigitalIdentity'ye bağlı ApplicationUser yok
        SetupActingUser();
        SetupPartyRoleRepository();
        SetupDigitalIdentity(currentStatus: GeneralStatus.Active, applicationUser: null);

        // Act
        var result = await _handler.Handle(BuildCommand(GeneralStatus.Passive), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(
            x => x.FindAsync(
                It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IOrderedQueryable<RefreshToken>>?>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
