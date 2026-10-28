using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Service.Common;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class LoginCommandHandlerTests
{
    private readonly Mock<IRepository<ApplicationUser, int>> _userRepositoryMock;
    private readonly Mock<IRepository<RefreshToken, int>>    _refreshTokenRepositoryMock;
    private readonly Mock<ITokenService>                     _tokenServiceMock;
    private readonly Mock<IPasswordHasher>                   _passwordHasherMock;
    private readonly Mock<ILoginAttemptLimiter>              _attemptLimiterMock;
    private readonly Mock<ILogger<LoginCommandHandler>>      _loggerMock;
    private readonly LoginCommandHandler                     _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock         = new Mock<IRepository<ApplicationUser, int>>();
        _refreshTokenRepositoryMock = new Mock<IRepository<RefreshToken, int>>();
        _tokenServiceMock           = new Mock<ITokenService>();
        _passwordHasherMock         = new Mock<IPasswordHasher>();
        _attemptLimiterMock         = new Mock<ILoginAttemptLimiter>();
        _loggerMock                 = new Mock<ILogger<LoginCommandHandler>>();

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _tokenServiceMock.Object,
            _passwordHasherMock.Object,
            _attemptLimiterMock.Object,   // GetLockoutRemaining/RegisterFailure varsayılanı null → kilit yok
            _loggerMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static ApplicationUser BuildUser(
        int userId              = 1,
        string email            = "john@example.com",
        string passwordHash     = "hashed_password",
        GeneralStatus status    = GeneralStatus.Active,
        int partyRoleTypeId     = ReferenceData.PartyRoleType.User)
    {
        return new ApplicationUser
        {
            Id          = userId,
            DigitalIdentity = new DigitalIdentity
            {
                Id       = Guid.NewGuid(),
                Nickname = "john_doe",
                Status   = status,
                Credentials = new List<Credential>
                {
                    new()
                    {
                        CredentialType = CredentialType.Password,
                        Characteristics = new List<CredentialCharacteristic>
                        {
                            new() { Name = "passwordHash", Value = passwordHash }
                        },
                        ContactMedia = new List<ContactMedium>
                        {
                            new()
                            {
                                MediumType  = ContactMediumType.EmailAddress,
                                IsPreferred = true,
                                Email       = email
                            }
                        }
                    }
                },
                PartyRole = new PartyRole
                {
                    Id              = 1,
                    PartyId         = 10,
                    PartyRoleTypeId = partyRoleTypeId,
                    Party   = new Party
                    {
                        Individual = new Individual
                        {
                            GivenName  = "John",
                            FamilyName = "Doe"
                        }
                    }
                },
            }
        };
    }

    private void SetupUser(ApplicationUser? user)
    {
        _userRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    private void SetupTokenService(
        string accessToken  = "access_token",
        string refreshToken = "refresh_token")
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
    public async Task Handle_ValidCredentials_ShouldReturnAuthResult()
    {
        // Arrange
        var user = BuildUser();
        SetupUser(user);
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify("Test1234!", "hashed_password"))
            .Returns(true);

        var command = new LoginCommand
        {
            Identifier = "john@example.com",
            Password   = "Test1234!"
        };

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.AccessTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        result.RefreshTokenExpiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldSaveRefreshToken()
    {
        // Arrange
        SetupUser(BuildUser());
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldSaveRefreshTokenWithCorrectUserId()
    {
        // Arrange
        SetupUser(BuildUser(userId: 42));
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        RefreshToken? capturedToken = null;
        _refreshTokenRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((rt, _) => capturedToken = rt);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        capturedToken.Should().NotBeNull();
        capturedToken!.ApplicationUserId.Should().Be(42);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupUser(null);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "notfound@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*bulunamadı*");
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupUser(BuildUser());
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "WrongPass!" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*hatalı*");
    }

    [Fact]
    public async Task Handle_SuspendedAccount_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupUser(BuildUser(status: GeneralStatus.Suspended));
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*aktif değil*");
    }

    [Fact]
    public async Task Handle_NoPasswordCredential_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = BuildUser();
        user.DigitalIdentity.Credentials.Clear();  // credential yok
        SetupUser(user);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*kimlik bilgisi*");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldResolveUserNameFromIndividual()
    {
        // Arrange
        SetupUser(BuildUser());
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert — CreateAccessToken'a "John Doe" geçilmeli
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken("John Doe", It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldUseNicknameWhenIndividualIsNull()
    {
        // Arrange
        var user = BuildUser();
        user.DigitalIdentity.PartyRole.Party.Individual = null;
        SetupUser(user);
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert — nickname kullanılmalı
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken("john_doe", It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    // ── Yeni: roleCd testleri ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCredentials_ShouldPassCorrectRoleCdToTokenService()
    {
        // Arrange — SiteAdmin rolüyle giriş yapan kullanıcı
        SetupUser(BuildUser(partyRoleTypeId: ReferenceData.PartyRoleType.SiteAdmin));
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert — CreateAccessToken'a doğru roleCd geçilmeli
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                ReferenceData.PartyRoleType.SiteAdminCd),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldPassUserRoleCdForRegularUser()
    {
        // Arrange — varsayılan (User) rolüyle giriş yapan kullanıcı
        SetupUser(BuildUser(partyRoleTypeId: ReferenceData.PartyRoleType.User));
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        _tokenServiceMock.Verify(
            x => x.CreateAccessToken(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                ReferenceData.PartyRoleType.UserCd),
            Times.Once);
    }

    // ── Deneme sınırı (ILoginAttemptLimiter) ─────────────────────────────

    // Handler anahtarı $"login:{normalize(identifier)}" biçiminde kuruyor.
    private const string LimiterKey = "login:john@example.com";

    private void VerifyUserLookup(Times times) =>
        _userRepositoryMock.Verify(x => x.FindOneAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
            It.IsAny<Func<IQueryable<ApplicationUser>, IQueryable<ApplicationUser>>?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), times);

    [Fact]
    public async Task Handle_AccountLocked_ShouldThrowEvenWithCorrectPasswordAndSkipLookup()
    {
        // Arrange — kilitliyken doğru şifre bile kabul edilmemeli
        _attemptLimiterMock
            .Setup(x => x.GetLockoutRemaining(LimiterKey))
            .Returns(TimeSpan.FromMinutes(5));
        SetupUser(BuildUser());
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert — kullanıcı sorgusuna ve şifre doğrulamaya hiç girilmemeli
        await act.Should().ThrowAsync<UnauthorizedException>();
        VerifyUserLookup(Times.Never());
        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldResetAttemptCounterAndNotRegisterFailure()
    {
        // Arrange
        SetupUser(BuildUser());
        SetupTokenService();
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        // Assert
        _attemptLimiterMock.Verify(x => x.Reset(LimiterKey), Times.Once);
        _attemptLimiterMock.Verify(x => x.RegisterFailure(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldRegisterFailureAndNotResetCounter()
    {
        // Arrange
        SetupUser(BuildUser());
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "WrongPass!" },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _attemptLimiterMock.Verify(x => x.RegisterFailure(LimiterKey), Times.Once);
        _attemptLimiterMock.Verify(x => x.Reset(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldStillRegisterFailure()
    {
        // Kullanıcı var olsun olmasın aynı davranış: sayaç yine artmalı
        // (aksi halde kilit davranışından hesabın varlığı sızar).
        SetupUser(null);

        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _attemptLimiterMock.Verify(x => x.RegisterFailure(LimiterKey), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPasswordCredential_ShouldRegisterFailure()
    {
        var user = BuildUser();
        user.DigitalIdentity.Credentials.Clear();
        SetupUser(user);

        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _attemptLimiterMock.Verify(x => x.RegisterFailure(LimiterKey), Times.Once);
    }

    [Fact]
    public async Task Handle_SuspendedAccount_ShouldNotRegisterFailure()
    {
        // "Hesap aktif değil" hatalı deneme sayılmaz (bkz. handler özeti).
        SetupUser(BuildUser(status: GeneralStatus.Suspended));

        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "Test1234!" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*aktif değil*");
        _attemptLimiterMock.Verify(x => x.RegisterFailure(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FailureTriggersLockout_ShouldThrowUnauthorizedException()
    {
        // RegisterFailure kilidi tetiklediğinde (TimeSpan döner) yine
        // UnauthorizedException fırlatılmalı; mesaj LoginAttemptPolicy'den gelir.
        SetupUser(BuildUser());
        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _attemptLimiterMock
            .Setup(x => x.RegisterFailure(LimiterKey))
            .Returns(TimeSpan.FromMinutes(15));

        var act = async () => await _handler.Handle(
            new LoginCommand { Identifier = "john@example.com", Password = "WrongPass!" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
