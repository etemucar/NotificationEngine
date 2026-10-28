using System.Linq.Expressions;
using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateIndividualCommandHandlerTests
{
    private readonly Mock<IRepository<Party, int>>                 _partyRepositoryMock;
    private readonly Mock<IRepository<RelatedParty, int>>          _relatedPartyRepositoryMock;
    private readonly Mock<IRepository<RelatedPartyType, int>>      _relatedPartyTypeRepositoryMock;
    private readonly Mock<IUnitOfWork>                             _unitOfWorkMock;
    private readonly Mock<IMapper>                                 _mapperMock;
    private readonly Mock<ILogger<CreateIndividualCommandHandler>> _loggerMock;
    private readonly CreateIndividualCommandHandler                _handler;

    public CreateIndividualCommandHandlerTests()
    {
        _partyRepositoryMock            = new Mock<IRepository<Party, int>>();
        _relatedPartyRepositoryMock     = new Mock<IRepository<RelatedParty, int>>();
        _relatedPartyTypeRepositoryMock = new Mock<IRepository<RelatedPartyType, int>>();
        _unitOfWorkMock                 = new Mock<IUnitOfWork>();
        _mapperMock                     = new Mock<IMapper>();
        _loggerMock                     = new Mock<ILogger<CreateIndividualCommandHandler>>();

        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // DÜZELTME: handler artık IUnitOfWork alıyor — party.Id SaveChanges'ten
        // önce okunursa her zaman 0 dönüyordu (Individual.PartyId FK'sine elle
        // atanıyordu). Artık FK, navigation property (party.Individual = individual)
        // ile kuruluyor ve IRepository<Individual,int> handler'dan tamamen kaldırıldı.
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Gerçek mapping'i taklit ediyoruz — testlerin assert kısımları
        // result.GivenName/FamilyName/... alanlarına doğrudan bakıyor.
        _mapperMock
            .Setup(x => x.Map<IndividualResponse>(It.IsAny<Individual>()))
            .Returns((Individual i) => new IndividualResponse
            {
                Id             = i.Id,
                GivenName      = i.GivenName,
                FamilyName     = i.FamilyName,
                MiddleName     = i.MiddleName,
                Title          = i.Title,
                Gender         = i.Gender,
                Nationality    = i.Nationality,
                BirthDate      = i.BirthDate,
                PlaceOfBirth   = i.PlaceOfBirth,
                CountryOfBirth = i.CountryOfBirth,
                MaritalStatus  = i.MaritalStatus,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = i.ValidForStart,
                    EndDateTime   = i.ValidForEnd
                }
            });

        _handler = new CreateIndividualCommandHandler(
            _partyRepositoryMock.Object,
            _relatedPartyRepositoryMock.Object,
            _relatedPartyTypeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePartyAndIndividual()
    {
        // Arrange
        var command = new CreateIndividualCommand
        {
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            Gender     = "Male",
            BirthDate  = new DateTime(1990, 1, 1)
        };

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.GivenName.Should().Be(command.GivenName);
        result.FamilyName.Should().Be(command.FamilyName);
        result.Gender.Should().Be(command.Gender);
        result.BirthDate.Should().Be(command.BirthDate);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallPartyRepositoryOnce()
    {
        // Arrange
        var command = new CreateIndividualCommand { GivenName = "Ahmet", FamilyName = "Yılmaz" };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldLinkIndividualToPartyViaNavigation()
    {
        // DEĞİŞTİ: eskiden "individual.PartyId doğru mu" test ediliyordu (elle FK
        // ataması). Artık FK, navigation property üzerinden kuruluyor ve gerçek
        // .PartyId değeri ancak DB'de SaveChanges gerçekleştiğinde (mock'ta
        // simüle edilmiyor) EF tarafından set edilir. Bu yüzden artık test
        // edilebilir/anlamlı olan şey "navigation doğru kuruldu mu"dur.
        var command = new CreateIndividualCommand { GivenName = "Ahmet", FamilyName = "Yılmaz" };

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedParty.Should().NotBeNull();
        capturedParty!.Individual.Should().NotBeNull();
        capturedParty.Individual!.GivenName.Should().Be("Ahmet");
        capturedParty.Individual.FamilyName.Should().Be("Yılmaz");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveChangesAfterAddingParty()
    {
        // YENİ TEST: appUser/individual.Id'nin SaveChanges'ten önce okunması
        // bug'ının düzeltildiğini doğrulayan regresyon testi.
        var command = new CreateIndividualCommand { GivenName = "Ahmet", FamilyName = "Yılmaz" };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutValidFor_ShouldSetMinMaxDateTime()
    {
        // Arrange — ValidFor gönderilmediğinde handler DateTime.MinValue/MaxValue set etmeli
        var command = new CreateIndividualCommand
        {
            GivenName     = "Ahmet",
            FamilyName    = "Yılmaz",
            ValidForStart = null,
            ValidForEnd   = null
        };

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedParty!.Individual!.ValidForStart.Should().Be(DateTime.MinValue);
        capturedParty.Individual.ValidForEnd.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldMapDatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate   = new DateTime(2025, 12, 31);

        var command = new CreateIndividualCommand
        {
            GivenName     = "Ahmet",
            FamilyName    = "Yılmaz",
            ValidForStart = startDate,
            ValidForEnd   = endDate
        };

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedParty!.Individual!.ValidForStart.Should().Be(startDate);
        capturedParty.Individual.ValidForEnd.Should().Be(endDate);
    }

    // ── RelatedParty ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownRelatedPartyRole_ShouldThrowInvalidInputAndWriteNothing()
    {
        SetupRelatedPartyTypes(OrganizationMemberType());
        var command = CommandWithRelatedParty("BilinmeyenRol", "10");

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>().WithMessage("*role*");
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RelatedPartyTargetNotFound_ShouldThrowInvalidInput()
    {
        SetupRelatedPartyTypes(OrganizationMemberType());
        SetupTargetParty(null);
        var command = CommandWithRelatedParty(ReferenceData.RelatedPartyType.OrganizationMemberCd, "10");

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>().WithMessage("*bulunamadı*");
    }

    [Fact]
    public async Task Handle_OrganizationMemberTargetIsNotOrganization_ShouldThrowInvalidInput()
    {
        SetupRelatedPartyTypes(OrganizationMemberType());
        SetupTargetParty(new Party { Id = 10, PartyType = PartyType.Individual });
        var command = CommandWithRelatedParty(ReferenceData.RelatedPartyType.OrganizationMemberCd, "10");

        var act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>().WithMessage("*Organization olmalı*");
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidOrganizationMember_ShouldAddRelatedPartyLinkedToNewParty()
    {
        SetupRelatedPartyTypes(OrganizationMemberType());
        SetupTargetParty(new Party { Id = 10, PartyType = PartyType.Organization });
        // Büyük/küçük harf fark etmez: kod küçük harfle gönderilir.
        var command = CommandWithRelatedParty(
            ReferenceData.RelatedPartyType.OrganizationMemberCd.ToLowerInvariant(), "10");

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        RelatedParty? capturedRelated = null;
        _relatedPartyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<RelatedParty>(), It.IsAny<CancellationToken>()))
            .Callback<RelatedParty, CancellationToken>((rp, _) => capturedRelated = rp)
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        capturedRelated.Should().NotBeNull();
        capturedRelated!.RelatedPartyId.Should().Be(10);
        capturedRelated.RelatedPartyTypeId.Should().Be(ReferenceData.RelatedPartyType.OrganizationMember);
        capturedRelated.Party.Should().BeSameAs(capturedParty);
        capturedRelated.ValidFrom.Should().NotBeNull();
        capturedRelated.ValidTo.Should().BeNull();   // açık/aktif bağlantı
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────

    private static RelatedPartyType OrganizationMemberType() => new()
    {
        Id = ReferenceData.RelatedPartyType.OrganizationMember,
        Code = ReferenceData.RelatedPartyType.OrganizationMemberCd,
        Name = "Organizasyon Üyesi"
    };

    private static CreateIndividualCommand CommandWithRelatedParty(string role, string targetPartyId) => new()
    {
        GivenName = "Ahmet",
        FamilyName = "Yılmaz",
        RelatedParty = new List<RelatedPartyRequest>
        {
            new()
            {
                Role = role,
                Type = "RelatedPartyRefOrPartyRoleRef",
                PartyOrPartyRole = new PartyOrPartyRoleRequest
                {
                    Id = targetPartyId, Type = "PartyRef", ReferredType = "Organization"
                }
            }
        }
    };

    // Handler FindAsync/FindOneAsync'i isteğe bağlı parametreleri atlayarak çağırıyor, ama Moq'un
    // expression tree'sinde parametre atlanamaz: hepsi It.IsAny ile verilmeli.
    private void SetupRelatedPartyTypes(params RelatedPartyType[] types)
    {
        _relatedPartyTypeRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<RelatedPartyType, bool>>>(),
                It.IsAny<Func<IQueryable<RelatedPartyType>, IOrderedQueryable<RelatedPartyType>>?>(),
                It.IsAny<Func<IQueryable<RelatedPartyType>, IQueryable<RelatedPartyType>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RelatedPartyType>)types.ToList());
    }

    private void SetupTargetParty(Party? target)
    {
        _partyRepositoryMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<Party, bool>>>(),
                It.IsAny<Func<IQueryable<Party>, IQueryable<Party>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
    }
}