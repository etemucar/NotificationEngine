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
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Requests;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class PatchDigitalIdentityCommandHandlerTests
{
    private readonly Mock<IRepository<DigitalIdentity, Guid>>          _digitalIdentityRepositoryMock;
    private readonly Mock<IRepository<Credential, Guid>>               _credentialRepositoryMock;
    private readonly Mock<IRepository<CredentialCharacteristic, int>>  _credentialCharacteristicRepositoryMock;
    private readonly Mock<IRepository<ContactMedium, int>>             _contactMediumRepositoryMock;
    private readonly Mock<IRepository<PartyRole, int>>                 _partyRoleRepositoryMock;
    private readonly Mock<ICurrentUserService>                         _currentUserServiceMock;
    private readonly Mock<IPasswordHasher>                             _passwordHasherMock;
    private readonly Mock<IMapper>                                     _mapperMock;
    private readonly Mock<ILogger<PatchDigitalIdentityCommandHandler>> _loggerMock;
    private readonly PatchDigitalIdentityCommandHandler                _handler;

    private const int  OwnerPartyRoleId  = 1;  // düzenlenen kaydın sahibi
    private const int  ActingPartyRoleId = 99; // isteği yapan kullanıcı
    private const int  PartyId           = 10;
    private static readonly Guid DigitalIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public PatchDigitalIdentityCommandHandlerTests()
    {
        _digitalIdentityRepositoryMock          = new Mock<IRepository<DigitalIdentity, Guid>>();
        _credentialRepositoryMock               = new Mock<IRepository<Credential, Guid>>();
        _credentialCharacteristicRepositoryMock = new Mock<IRepository<CredentialCharacteristic, int>>();
        _contactMediumRepositoryMock            = new Mock<IRepository<ContactMedium, int>>();
        _partyRoleRepositoryMock                = new Mock<IRepository<PartyRole, int>>();
        _currentUserServiceMock                 = new Mock<ICurrentUserService>();
        _passwordHasherMock                     = new Mock<IPasswordHasher>();
        _mapperMock                              = new Mock<IMapper>();
        _loggerMock                              = new Mock<ILogger<PatchDigitalIdentityCommandHandler>>();

        // Handler'ı AutoMapper profilinden izole tutmak için manuel mapping — sadece
        // testlerin assert ettiği alanları taşıyor.
        _mapperMock
            .Setup(x => x.Map<DigitalIdentityResponse>(It.IsAny<DigitalIdentity>()))
            .Returns((DigitalIdentity src) => new DigitalIdentityResponse
            {
                Id       = src.Id,
                Nickname = src.Nickname
            });

        _handler = new PatchDigitalIdentityCommandHandler(
            _digitalIdentityRepositoryMock.Object,
            _credentialRepositoryMock.Object,
            _credentialCharacteristicRepositoryMock.Object,
            _contactMediumRepositoryMock.Object,
            _partyRoleRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _passwordHasherMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private void SetupActingUser(int actingPartyRoleId)
    {
        _currentUserServiceMock
            .Setup(x => x.GetPartyRoleIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(actingPartyRoleId);
    }

    private void SetupPartyRoleRepository(bool actingIsSiteAdmin)
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

    private static DigitalIdentity BuildDigitalIdentity(
        int ownerPartyRoleId = OwnerPartyRoleId,
        List<Credential>? credentials = null)
    {
        return new DigitalIdentity
        {
            Id          = DigitalIdentityId,
            Nickname    = "old_nickname",
            PartyRoleId = ownerPartyRoleId,
            PartyRole   = new PartyRole { Id = ownerPartyRoleId, PartyId = PartyId },
            Credentials = credentials ?? new List<Credential>()
        };
    }

    private void SetupDigitalIdentity(DigitalIdentity? digitalIdentity)
    {
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
                if (digitalIdentity is null) return null;
                var compiled = predicate.Compile();
                return compiled(digitalIdentity) ? digitalIdentity : null;
            });
    }

    private static PatchDigitalIdentityCommand BuildCommand(
        string? nickname = "new_nickname",
        List<CredentialPatchRequest>? credentials = null,
        Guid? digitalIdentityId = null)
    {
        return new PatchDigitalIdentityCommand
        {
            DigitalIdentityId = digitalIdentityId ?? DigitalIdentityId,
            Nickname          = nickname,
            Credentials       = credentials
        };
    }

    // Not: Patch akışında dış tip CredentialPatchRequest (Features.Commands) — ama iç Characteristics/
    // ContactMedia koleksiyonları handler'da CredentialCharacteristicRequest/ContactMediumRequest
    // (Dtos.Requests) ile map ediliyor (bkz. PatchDigitalIdentityCommandHandler.MapCharacteristic /
    // MapContactMedium imzaları), o yüzden burada da aynı iç tipler kullanılıyor.
    private static CredentialPatchRequest BuildCredentialRequest(
        Guid? id = null,
        string password = "NewPass1!",
        string email = "new@example.com")
    {
        return new CredentialPatchRequest
        {
            Id              = id,
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
                    MediumType     = "EmailAddress",
                    Preferred      = true,
                    Characteristic = new Dictionary<string, object> { { "emailAddress", email } }
                }
            }
        };
    }

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OwnRecord_ShouldUpdateNicknameAndReturnResponse()
    {
        // Arrange — çağıran kendi kaydını düzenliyor, SITE_ADMIN olmasına gerek yok
        SetupActingUser(OwnerPartyRoleId);
        var digitalIdentity = BuildDigitalIdentity();
        SetupDigitalIdentity(digitalIdentity);

        // Act
        var result = await _handler.Handle(BuildCommand(nickname: "new_nickname"), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Nickname.Should().Be("new_nickname");
        _digitalIdentityRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<DigitalIdentity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotOwnRecordButSiteAdmin_ShouldSucceed()
    {
        // Arrange
        SetupActingUser(ActingPartyRoleId);
        SetupPartyRoleRepository(actingIsSiteAdmin: true);
        var digitalIdentity = BuildDigitalIdentity(ownerPartyRoleId: OwnerPartyRoleId);
        SetupDigitalIdentity(digitalIdentity);

        // Act
        var result = await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotOwnRecordAndNotSiteAdmin_ShouldThrowUnauthorizedException()
    {
        // Arrange
        SetupActingUser(ActingPartyRoleId);
        SetupPartyRoleRepository(actingIsSiteAdmin: false);
        var digitalIdentity = BuildDigitalIdentity(ownerPartyRoleId: OwnerPartyRoleId);
        SetupDigitalIdentity(digitalIdentity);

        // Act
        var act = async () => await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

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
        SetupDigitalIdentity(null);

        // Act
        var act = async () => await _handler.Handle(BuildCommand(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CredentialsNull_ShouldOnlyUpdateNicknameAndNotTouchCredentialRepository()
    {
        // Arrange
        SetupActingUser(OwnerPartyRoleId);
        var digitalIdentity = BuildDigitalIdentity();
        SetupDigitalIdentity(digitalIdentity);

        // Act
        await _handler.Handle(BuildCommand(nickname: "just_nickname", credentials: null), TestContext.Current.CancellationToken);

        // Assert
        _credentialRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Never);
        _credentialRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewCredentialWithoutId_ShouldAddCredentialAndHashPassword()
    {
        // Arrange — gelen credential listesinde Id yok → yeni credential olarak eklenmeli
        SetupActingUser(OwnerPartyRoleId);
        var digitalIdentity = BuildDigitalIdentity();
        SetupDigitalIdentity(digitalIdentity);

        _passwordHasherMock.Setup(x => x.Hash("NewPass1!")).Returns("hashed_new_pass");

        Credential? captured = null;
        _credentialRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()))
            .Callback<Credential, CancellationToken>((c, _) => captured = c);

        var command = BuildCommand(credentials: new List<CredentialPatchRequest> { BuildCredentialRequest(id: null) });

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        captured.Should().NotBeNull();
        captured!.Characteristics.First(c => c.Name == "passwordHash").Value.Should().Be("hashed_new_pass");
        captured.ContactMedia.First().Email.Should().Be("new@example.com");
        captured.ContactMedia.First().PartyId.Should().Be(PartyId);
        _credentialRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCredentialId_ShouldReplaceCharacteristicsAndContactMedia()
    {
        // Arrange — gelen credential'ın Id'si mevcut bir kayda eşleşiyor → tam replace beklenir
        SetupActingUser(OwnerPartyRoleId);

        var existingCredentialId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var existingCredential = new Credential
        {
            Id              = existingCredentialId,
            CredentialType  = CredentialType.Password,
            TrustLevel      = 1,
            Characteristics = new List<CredentialCharacteristic>
            {
                new() { Id = 1, Name = "passwordHash", Value = "old_hash" }
            },
            ContactMedia = new List<ContactMedium>
            {
                new() { Id = 1, PartyId = PartyId, MediumType = ContactMediumType.EmailAddress, Email = "old@example.com" }
            }
        };

        var digitalIdentity = BuildDigitalIdentity(credentials: new List<Credential> { existingCredential });
        SetupDigitalIdentity(digitalIdentity);

        _passwordHasherMock.Setup(x => x.Hash("NewPass1!")).Returns("hashed_new_pass");

        var command = BuildCommand(credentials: new List<CredentialPatchRequest>
        {
            BuildCredentialRequest(id: existingCredentialId)
        });

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        existingCredential.Characteristics.Should().ContainSingle(c => c.Name == "passwordHash" && c.Value == "hashed_new_pass");
        existingCredential.ContactMedia.Should().ContainSingle(cm => cm.Email == "new@example.com");
        _credentialCharacteristicRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<CredentialCharacteristic>(), It.IsAny<CancellationToken>()), Times.Once);
        _contactMediumRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<ContactMedium>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepositoryMock.Verify(
            x => x.UpdateAsync(existingCredential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CredentialMissingFromIncomingList_ShouldRemoveCredentialAndItsContactMedia()
    {
        // Arrange — mevcut credential gelen listede yok → silinmeli (contact medium'larıyla birlikte)
        SetupActingUser(OwnerPartyRoleId);

        var staleCredential = new Credential
        {
            Id           = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Characteristics = new List<CredentialCharacteristic>(),
            ContactMedia = new List<ContactMedium>
            {
                new() { Id = 2, PartyId = PartyId, MediumType = ContactMediumType.EmailAddress, Email = "stale@example.com" }
            }
        };

        var digitalIdentity = BuildDigitalIdentity(credentials: new List<Credential> { staleCredential });
        SetupDigitalIdentity(digitalIdentity);

        var command = BuildCommand(credentials: new List<CredentialPatchRequest>()); // boş liste — mevcut kayıt silinmeli

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        digitalIdentity.Credentials.Should().NotContain(staleCredential);
        _contactMediumRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<ContactMedium>(), It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepositoryMock.Verify(
            x => x.RemoveAsync(staleCredential, It.IsAny<CancellationToken>()), Times.Once);
    }
}
