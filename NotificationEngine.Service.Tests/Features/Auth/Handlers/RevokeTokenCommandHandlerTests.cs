using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IRepository<RefreshToken, int>>   _refreshTokenRepositoryMock;
    private readonly Mock<ILogger<RevokeTokenCommandHandler>> _loggerMock;
    private readonly RevokeTokenCommandHandler              _handler;

    public RevokeTokenCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRepository<RefreshToken, int>>();
        _loggerMock                 = new Mock<ILogger<RevokeTokenCommandHandler>>();

        _handler = new RevokeTokenCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static RefreshToken BuildActiveToken(string tokenStr = "valid_token") => new()
    {
        Id        = 1,
        Token     = tokenStr,
        IsRevoked = false,
        ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    private static RefreshToken BuildRevokedToken(string tokenStr = "revoked_token") => new()
    {
        Id        = 2,
        Token     = tokenStr,
        IsRevoked = true,
        ExpiresAt = DateTime.UtcNow.AddDays(30)
    };

    private void SetupExistingToken(RefreshToken? token)
    {
        _refreshTokenRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                It.IsAny<Func<IQueryable<RefreshToken>, IQueryable<RefreshToken>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveToken_ShouldReturnTrue()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());

        // Act
        var result = await _handler.Handle(
            new RevokeTokenCommand { RefreshToken = "valid_token" },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ActiveToken_ShouldSetIsRevokedTrue()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());

        RefreshToken? capturedToken = null;
        _refreshTokenRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((rt, _) => capturedToken = rt);

        // Act
        await _handler.Handle(
            new RevokeTokenCommand { RefreshToken = "valid_token" },
            TestContext.Current.CancellationToken);

        // Assert
        capturedToken.Should().NotBeNull();
        capturedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ShouldReturnTrueWithoutUpdate()
    {
        // Arrange
        SetupExistingToken(BuildRevokedToken());

        // Act
        var result = await _handler.Handle(
            new RevokeTokenCommand { RefreshToken = "revoked_token" },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupExistingToken(null);

        // Act
        var act = async () => await _handler.Handle(
            new RevokeTokenCommand { RefreshToken = "nonexistent_token" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*bulunamadı*");
    }

    [Fact]
    public async Task Handle_ActiveToken_ShouldCallUpdateOnce()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());

        // Act
        await _handler.Handle(
            new RevokeTokenCommand { RefreshToken = "valid_token" },
            TestContext.Current.CancellationToken);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
