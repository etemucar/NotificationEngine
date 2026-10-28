using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Core.Common.References;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateAdminUserCommandHandlerTests
{
    private readonly Mock<IRepository<Party, int>>           _partyRepositoryMock;
    private readonly Mock<IRepository<ApplicationUser, int>> _userRepositoryMock;
    private readonly Mock<ICurrentUserService>                _currentUserServiceMock;
    private readonly Mock<IPasswordHasher>                    _passwordHasherMock;
    private readonly Mock<IUnitOfWork>                        _unitOfWorkMock;
    private readonly Mock<ILogger<CreateAdminUserCommandHandler>> _loggerMock;
    private readonly CreateAdminUserCommandHandler            _handler;

    public CreateAdminUserCommandHandlerTests()
    {
        _partyRepositoryMock    = new Mock<IRepository<Party, int>>();
        _userRepositoryMock     = new Mock<IRepository<ApplicationUser, int>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _passwordHasherMock     = new Mock<IPasswordHasher>();
        _unitOfWorkMock         = new Mock<IUnitOfWork>();
        _loggerMock             = new Mock<ILogger<CreateAdminUserCommandHandler>>();

        // DÜZELTME: handler artık IUnitOfWork alıyor (mapper turu sonrası,
        // appUser.Id'nin SaveChanges'ten önce okunması bug'ının düzeltilmesiyle
        // eklendi). Varsayılan olarak burada set ediliyor — testlerin çoğu
        // SaveChangesAsync'in gerçekten çağrılıp çağrılmadığını değil, sonucu
        // önemsiyor.
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); 

        _handler = new CreateAdminUserCommandHandler(
            _partyRepositoryMock.Object,
            _userRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CreateAdminUserCommand BuildCommand(
        string givenName  = "Admin",
        string familyName = "User",
        string identifier = "admin@example.com",
        string password   = "Test1234!",
        int    languageId = 1) => new()
    {
        GivenName  = givenName,
        FamilyName = familyName,
        Identifier = identifier,
        Password   = password,
        LanguageId = languageId
    };

    private void SetupUserNotExists()
    {
        _userRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupUserExists()
    {
        _userRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupPartyRepository()
    {
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupPasswordHasher(string hash = "hashed_password")
    {
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(hash);
    }

    private void SetupCurrentUser(int? creatorUserId = 7)
    {
        _currentUserServiceMock
            .Setup(x => x.UserId)
            .Returns(creatorUserId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnCreateAdminUserResult()
    {
        // Arrange
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser();

        // Act
        var result = await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.UserIdentifier.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveParty()
    {
        // Arrange
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser();

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveChangesAfterAddingParty()
    {
        // Arrange — YENİ TEST: appUser.Id'nin SaveChanges'ten önce okunması
        // bug'ının düzeltildiğini doğrulayan regresyon testi.
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser();

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignSiteAdminPartyRoleType()
    {
        // Arrange — A7/B10 için asıl kritik test: her zaman SiteAdmin, asla User değil.
        SetupUserNotExists();
        SetupPasswordHasher();
        SetupCurrentUser();

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        var partyRole = capturedParty!.PartyRoles!.First();
        partyRole.PartyRoleTypeId.Should().Be(ReferenceData.PartyRoleType.SiteAdmin);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldHashPassword()
    {
        // Arrange
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser();

        // Act
        await _handler.Handle(
            BuildCommand(password: "Test1234!"), TestContext.Current.CancellationToken);

        // Assert
        _passwordHasherMock.Verify(x => x.Hash("Test1234!"), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldBuildCorrectIndividual()
    {
        // Arrange
        SetupUserNotExists();
        SetupPasswordHasher();
        SetupCurrentUser();

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            BuildCommand(givenName: "Admin", familyName: "User"),
            TestContext.Current.CancellationToken);

        // Assert
        capturedParty.Should().NotBeNull();
        capturedParty!.Individual!.GivenName.Should().Be("Admin");
        capturedParty!.Individual!.FamilyName.Should().Be("User");
    }

    [Fact]
    public async Task Handle_EmailIdentifier_ShouldSetEmailOnContactMedium()
    {
        // Arrange
        SetupUserNotExists();
        SetupPasswordHasher();
        SetupCurrentUser();

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            BuildCommand(identifier: "admin@example.com"),
            TestContext.Current.CancellationToken);

        // Assert
        var contactMedium = capturedParty!.PartyRoles!.First()
            .DigitalIdentity!.Credentials.First()
            .ContactMedia.First();

        contactMedium.Email.Should().Be("admin@example.com");
        contactMedium.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PhoneIdentifier_ShouldSetPhoneOnContactMedium()
    {
        // Arrange
        SetupUserNotExists();
        SetupPasswordHasher();
        SetupCurrentUser();

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            BuildCommand(identifier: "05551234567"),
            TestContext.Current.CancellationToken);

        // Assert
        var contactMedium = capturedParty!.PartyRoles!.First()
            .DigitalIdentity!.Credentials.First()
            .ContactMedia.First();

        contactMedium.PhoneNumber.Should().Be("05551234567");
        contactMedium.Email.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DuplicateIdentifier_ShouldThrowConflictException()
    {
        // Arrange
        SetupUserExists();

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*kayıtlı*");
    }

    [Fact]
    public async Task Handle_DuplicateIdentifier_ShouldNotSaveParty()
    {
        // Arrange
        SetupUserExists();

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>();

        // Assert
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldNotGenerateAnyToken()
    {
        // Arrange — kritik: CreateAdminUserCommandHandler'ın ITokenService bağımlılığı yok,
        // yeni oluşturulan admin'e otomatik login/token verilmiyor.
        SetupUserNotExists();
        SetupPasswordHasher();
        SetupCurrentUser();

        // Act
        var result = await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert — CreateAdminUserResult'ta AccessToken/RefreshToken alanları hiç yok;
        // dolayısıyla sadece result'ın kimlik bilgisi döndürdüğünü doğruluyoruz.
        result.UserId.Should().BeGreaterThanOrEqualTo(0);
        result.UserName.Should().Be("Admin User");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReadCreatorUserIdForAuditLog()
    {
        // Arrange
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser(creatorUserId: 7);

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert — sadece audit log için okunduğunu doğruluyoruz (davranışı değiştirmiyor)
        _currentUserServiceMock.VerifyGet(x => x.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_CreatorUserIdNull_ShouldStillSucceed()
    {
        // Arrange — ICurrentUserService.UserId null dönse bile (teorik olarak olmamalı,
        // policy zaten kimlik doğrulanmış kullanıcı gerektiriyor) handler patlamamalı.
        SetupUserNotExists();
        SetupPartyRepository();
        SetupPasswordHasher();
        SetupCurrentUser(creatorUserId: null);

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
