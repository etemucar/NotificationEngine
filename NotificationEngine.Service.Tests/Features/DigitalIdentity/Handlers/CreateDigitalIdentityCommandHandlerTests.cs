using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateDigitalIdentityCommandHandlerTests
{
    private readonly Mock<IRepository<DigitalIdentity, Guid>> _digitalIdentityRepositoryMock;
    private readonly Mock<IRepository<ApplicationUser, int>>  _userRepositoryMock;
    private readonly Mock<IRepository<PartyRole, int>>        _partyRoleRepositoryMock;
    private readonly Mock<ICurrentUserService>                _currentUserServiceMock;
    private readonly Mock<IPasswordHasher>                    _passwordHasherMock;
    private readonly Mock<IMapper>                            _mapperMock;
    private readonly Mock<ILogger<CreateDigitalIdentityCommandHandler>> _loggerMock;
    private readonly CreateDigitalIdentityCommandHandler      _handler;

    private const int ActingPartyRoleId = 99; // yetki kontrolünü yapan (çağıran) kullanıcının PartyRoleId'si

    public CreateDigitalIdentityCommandHandlerTests()
    {
        _digitalIdentityRepositoryMock = new Mock<IRepository<DigitalIdentity, Guid>>();
        _userRepositoryMock            = new Mock<IRepository<ApplicationUser, int>>();
        _partyRoleRepositoryMock       = new Mock<IRepository<PartyRole, int>>();
        _currentUserServiceMock        = new Mock<ICurrentUserService>();
        _passwordHasherMock            = new Mock<IPasswordHasher>();
        _mapperMock                    = new Mock<IMapper>();
        _loggerMock                    = new Mock<ILogger<CreateDigitalIdentityCommandHandler>>();

        // DÜZELTME: handler artık IMapper alıyor (mapper standardizasyon turu —
        // static MapToResponse yerine CreateMap<DigitalIdentity, DigitalIdentityResponse>
        // kullanılıyor). Testlerin Assert kısımları result.Nickname/Status/PartyRoleId
        // gibi alanlara baktığı için, gerçek mapping'i burada TAKLİT ediyoruz —
        // aksi halde Moq varsayılan olarak null/default dönüp assertion'ları kırar.
        _mapperMock
            .Setup(m => m.Map<DigitalIdentityResponse>(It.IsAny<DigitalIdentity>()))
            .Returns((DigitalIdentity d) => new DigitalIdentityResponse
            {
                Id                  = d.Id,
                Nickname            = d.Nickname,
                Status              = d.Status,
                DigitalIdentityDate = d.DigitalIdentityDate,
                PartyRoleId         = d.PartyRoleId
            });

        _handler = new CreateDigitalIdentityCommandHandler(
            _digitalIdentityRepositoryMock.Object,
            _userRepositoryMock.Object,
            _partyRoleRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _passwordHasherMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Handler iki farklı FindOneAsync çağrısı yapıyor: biri çağıranın rolünü
    /// (yetki kontrolü için), biri hedef PartyRoleId'yi bulmak için. Aynı mock'ta
    /// tek Setup ile ikisini ayırt etmek için gelen predicate'i, elimizdeki
    /// bilinen PartyRole nesnelerine karşı derleyip çalıştırıyoruz.
    /// </summary>
    private void SetupPartyRoleRepository(
        bool actingIsSiteAdmin = true,
        int  targetPartyRoleId = 1,
        int  targetPartyId     = 10)
    {
        var actingRole = new PartyRole
        {
            Id = ActingPartyRoleId,
            PartyRoleType = new PartyRoleType
            {
                PartyRoleTypeCd = actingIsSiteAdmin ? "SITE_ADMIN" : "USER"
            }
        };

        var targetRole = new PartyRole
        {
            Id      = targetPartyRoleId,
            PartyId = targetPartyId
        };

        var knownRoles = new[] { actingRole, targetRole };

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
                return knownRoles.FirstOrDefault(compiled);
            });
    }

    private void SetupActingUser(int actingPartyRoleId = ActingPartyRoleId)
    {
        _currentUserServiceMock
            .Setup(x => x.GetPartyRoleIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(actingPartyRoleId);
    }

    private CreateDigitalIdentityCommand BuildCommand(
        int partyRoleId = 1,
        string? nickname = "john_doe",
        string password = "Test1234!")
    {
        return new CreateDigitalIdentityCommand
        {
            Nickname    = nickname,
            PartyRoleId = partyRoleId,
            Credentials = new List<CredentialRequest>
            {
                new()
                {
                    CredentialType  = CredentialType.Password,
                    TrustLevel      = 1,
                    Characteristics = new List<CredentialCharacteristicRequest>
                    {
                        new() { Name = "password", Value = password }
                    },
                    ContactMedia = new List<ContactMediumRequest>
                    {
                        new()
                        {
                            MediumType  = "EmailAddress",
                            Preferred   = true,
                            Characteristic = new Dictionary<string, object>
                            {
                                { "emailAddress", "john@example.com" }
                            }
                        }
                    }
                }
            }
        };
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateDigitalIdentityAndApplicationUser()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var command = BuildCommand();

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Nickname.Should().Be(command.Nickname);
        result.PartyRoleId.Should().Be(command.PartyRoleId);
        result.Status.Should().Be(GeneralStatus.Active);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallDigitalIdentityRepositoryOnce()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        _digitalIdentityRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallApplicationUserRepositoryOnce()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldHashPassword()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository();
        DigitalIdentity? capturedDigitalIdentity = null;

        _digitalIdentityRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<DigitalIdentity, CancellationToken>((di, _) => capturedDigitalIdentity = di);

        _passwordHasherMock
            .Setup(x => x.Hash("Test1234!"))
            .Returns("hashed_password");

        // Act
        await _handler.Handle(BuildCommand(password: "Test1234!"), TestContext.Current.CancellationToken);

        // Assert
        capturedDigitalIdentity.Should().NotBeNull();

        var passwordHash = capturedDigitalIdentity!.Credentials
            .First()
            .Characteristics
            .First(ch => ch.Name == "passwordHash")
            .Value;

        passwordHash.Should().Be("hashed_password");
        _passwordHasherMock.Verify(x => x.Hash("Test1234!"), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldMapEmailContactMedium()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository(targetPartyId: 10);
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        DigitalIdentity? capturedDigitalIdentity = null;
        _digitalIdentityRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<DigitalIdentity, CancellationToken>((di, _) => capturedDigitalIdentity = di);

        // Act
        await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        var contactMedium = capturedDigitalIdentity!.Credentials
            .First()
            .ContactMedia
            .First();

        contactMedium.Email.Should().Be("john@example.com");
        contactMedium.MediumType.Should().Be(ContactMediumType.EmailAddress);
        contactMedium.IsPreferred.Should().BeTrue();
        contactMedium.PartyId.Should().Be(10);
    }

    [Fact]
    public async Task Handle_PartyRoleNotFound_ShouldThrowNotFoundException()
    {
        // Arrange — çağıran yetkili (SITE_ADMIN), ama hedef PartyRoleId hiç yok
        SetupActingUser();
        SetupPartyRoleRepository(targetPartyRoleId: 1); // knownRoles'te sadece Id=1 var

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(partyRoleId: 999),
            TestContext.Current.CancellationToken);

        // Assert — handler artık KeyNotFoundException değil NotFoundException fırlatıyor
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    // ── Yeni: yetki kontrolü testleri (B5) ──────────────────────────────

    [Fact]
    public async Task Handle_ActingUserNotSiteAdmin_ShouldThrowUnauthorizedException()
    {
        // Arrange — çağıran kullanıcı SITE_ADMIN değil
        SetupActingUser();
        SetupPartyRoleRepository(actingIsSiteAdmin: false);
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*yetkiniz yok*");
    }

    [Fact]
    public async Task Handle_ActingUserNotSiteAdmin_ShouldNotCreateDigitalIdentity()
    {
        // Arrange
        SetupActingUser();
        SetupPartyRoleRepository(actingIsSiteAdmin: false);

        // Act
        var act = async () => await _handler.Handle(
            BuildCommand(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();

        // Assert — yetkisiz istekte hiçbir kayıt oluşturulmamalı
        _digitalIdentityRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActingUserIsSiteAdmin_ShouldSucceed()
    {
        // Arrange — pozitif kontrol testi
        SetupActingUser();
        SetupPartyRoleRepository(actingIsSiteAdmin: true);
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        // Act
        var result = await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }
}
