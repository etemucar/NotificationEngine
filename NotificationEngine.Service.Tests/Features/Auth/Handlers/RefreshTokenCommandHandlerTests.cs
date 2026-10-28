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
using NotificationEngine.Core.Common.References;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRepository<RefreshToken, int>>    _refreshTokenRepositoryMock;
    private readonly Mock<IRepository<ApplicationUser, int>> _userRepositoryMock;
    private readonly Mock<ITokenService>                     _tokenServiceMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock;
    private readonly RefreshTokenCommandHandler              _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRepository<RefreshToken, int>>();
        _userRepositoryMock         = new Mock<IRepository<ApplicationUser, int>>();
        _tokenServiceMock           = new Mock<ITokenService>();
        _loggerMock                 = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static RefreshToken BuildActiveToken(
        int userId          = 1,
        string tokenStr     = "valid_refresh_token",
        int partyRoleTypeId = ReferenceData.PartyRoleType.User)
    {
        return new RefreshToken
        {
            Id                = 1,
            Token             = tokenStr,
            IsRevoked         = false,
            ExpiresAt         = DateTime.UtcNow.AddDays(30),
            ApplicationUserId = userId,
            ApplicationUser   = new ApplicationUser
            {
                Id = userId,
                DigitalIdentity = new DigitalIdentity
                {
                    Id       = Guid.NewGuid(),
                    Nickname = "john_doe",
                    PartyRole = new PartyRole
                    {
                        PartyRoleTypeId = partyRoleTypeId,
                        Party = new Party
                        {
                            Individual = new Individual
                            {
                                GivenName  = "John",
                                FamilyName = "Doe"
                            }
                        }
                    },
                    Credentials = new List<Credential>
                    {
                        new()
                        {
                            CredentialType = CredentialType.Password,
                            ContactMedia   = new List<ContactMedium>
                            {
                                new()
                                {
                                    Email      = "john@example.com",
                                    MediumType = ContactMediumType.EmailAddress
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static RefreshToken BuildExpiredToken(string tokenStr = "expired_token")
    {
        var token = BuildActiveToken(tokenStr: tokenStr);
        token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        return token;
    }

    private static RefreshToken BuildRevokedToken(string tokenStr = "revoked_token")
    {
        var token = BuildActiveToken(tokenStr: tokenStr);
        token.IsRevoked = true;
        return token;
    }

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

    private void SetupTokenService(
        string accessToken  = "new_access_token",
        string refreshToken = "new_refresh_token")
    {
        _tokenServiceMock
            .Setup(x => x.CreateAccessToken(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(new AccessToken
            {
                Token      = accessToken,
                Expiration = DateTime.UtcNow.AddDays(1)
            });

        _tokenServiceMock
            .Setup(x => x.CreateRefreshToken(It.IsAny<int>()))
            .Returns(new RefreshToken
            {
                Token     = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidToken_ShouldReturnNewAuthResult()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());
        SetupTokenService();

        // Act
        var result = await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        result.AccessTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        result.RefreshTokenExpiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldRevokeOldToken()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());
        SetupTokenService();

        RefreshToken? capturedToken = null;
        _refreshTokenRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((rt, _) => capturedToken = rt);

        // Act
        await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        capturedToken.Should().NotBeNull();
        capturedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldSaveNewRefreshToken()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());
        SetupTokenService();

        // Act
        await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldResolveUserNameFromIndividual()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken(userId: 1));
        SetupTokenService();

        // Act
        await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken("John Doe", It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldUseNicknameWhenIndividualIsNull()
    {
        // Arrange
        var token = BuildActiveToken();
        token.ApplicationUser.DigitalIdentity.PartyRole!.Party.Individual = null;
        SetupExistingToken(token);
        SetupTokenService();

        // Act
        await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken("john_doe", It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupExistingToken(null);

        // Act
        var act = async () => await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "nonexistent_token" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*bulunamadı*");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupExistingToken(BuildExpiredToken());

        // Act
        var act = async () => await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "expired_token" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*geçersiz*");
    }

    [Fact]
    public async Task Handle_RevokedToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupExistingToken(BuildRevokedToken());

        // Act
        var act = async () => await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "revoked_token" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*geçersiz*");
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldNotSaveNewTokenOnRevocationFailure()
    {
        // Arrange
        SetupExistingToken(BuildActiveToken());
        _refreshTokenRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB hatası"));

        // Act
        var act = async () => await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert — update patlarsa yeni token kaydedilmemeli
        await act.Should().ThrowAsync<Exception>();
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Yeni: roleCd testi ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidToken_ShouldPassCorrectRoleCdToTokenService()
    {
        // Arrange — SiteAdmin rolüne sahip kullanıcı token'ı yeniliyor
        SetupExistingToken(BuildActiveToken(partyRoleTypeId: ReferenceData.PartyRoleType.SiteAdmin));
        SetupTokenService();

        // Act
        await _handler.Handle(
            new RefreshTokenCommand { RefreshToken = "valid_refresh_token" },
            TestContext.Current.CancellationToken);

        // Assert
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                ReferenceData.PartyRoleType.SiteAdminCd),
            Times.Once);
    }
}
