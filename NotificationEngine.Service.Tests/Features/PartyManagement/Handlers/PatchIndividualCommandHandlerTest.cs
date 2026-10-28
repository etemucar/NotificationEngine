using System.Linq.Expressions;
using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Requests;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class PatchIndividualCommandHandlerTests
{
    private readonly Mock<IRepository<Individual, int>>            _individualRepositoryMock;
    private readonly Mock<IRepository<ContactMedium, int>>         _contactMediumRepositoryMock;
    private readonly Mock<IMapper>                                 _mapperMock;
    private readonly Mock<ILogger<PatchIndividualCommandHandler>>  _loggerMock;
    private readonly PatchIndividualCommandHandler                 _handler;

    public PatchIndividualCommandHandlerTests()
    {
        _individualRepositoryMock    = new Mock<IRepository<Individual, int>>();
        _contactMediumRepositoryMock = new Mock<IRepository<ContactMedium, int>>();
        _mapperMock                  = new Mock<IMapper>();
        _loggerMock                  = new Mock<ILogger<PatchIndividualCommandHandler>>();

        // Handler'ı AutoMapper profilinden izole tutmak için manuel mapping — sadece
        // testlerin assert ettiği alanları taşıyor.
        _mapperMock
            .Setup(x => x.Map<IndividualResponse>(It.IsAny<Individual>()))
            .Returns((Individual src) => new IndividualResponse
            {
                Id         = src.Id,
                GivenName  = src.GivenName,
                FamilyName = src.FamilyName,
                ValidFor   = new TimePeriodResponse
                {
                    StartDateTime = src.ValidForStart,
                    EndDateTime   = src.ValidForEnd
                }
            });

        _handler = new PatchIndividualCommandHandler(
            _individualRepositoryMock.Object,
            _contactMediumRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingIndividual_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var existing = new Individual
        {
            Id         = 1,
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue

        };

        var command = new PatchIndividualCommand
        {
            Id        = 1,
            GivenName = "Mehmet"
        };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _individualRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.GivenName.Should().Be("Mehmet");
        result.FamilyName.Should().Be("Yılmaz"); // değişmemeli
    }

    [Fact]
    public async Task Handle_NonExistingIndividual_ShouldThrowNotFoundException()
    {
        // Arrange — handler artık null dönmüyor, NotFoundException fırlatıyor
        // (bkz. PatchIndividualCommandHandler.cs satır 34)
        var command = new PatchIndividualCommand { Id = 99 };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Individual?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldUpdateDates()
    {
        // Arrange
        var existing = new Individual
        {
            Id         = 1,
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        var newStart = new DateTime(2024, 1, 1);
        var newEnd   = new DateTime(2025, 12, 31);

        var command = new PatchIndividualCommand
        {
            Id       = 1,
            ValidForStart = newStart, 
            ValidForEnd = newEnd
        };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _individualRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ValidFor.StartDateTime.Should().Be(newStart);
        result.ValidFor.EndDateTime.Should().Be(newEnd);
    }

    [Fact]
    public async Task Handle_ShouldCallUpdateRepositoryOnce()
    {
        // Arrange
        var existing = new Individual
        {
            Id         = 1,
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        var command = new PatchIndividualCommand { Id = 1, GivenName = "Mehmet" };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _individualRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _individualRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ContactMedium: gönderildiyse "sil ve yeniden yükle" ──────────────

    [Fact]
    public async Task Handle_WithoutContactMedium_ShouldNotTouchContactMedia()
    {
        SetupIndividual(partyId: 5);
        var command = new PatchIndividualCommand { Id = 1, GivenName = "Mehmet" };   // ContactMedium = null

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _contactMediumRepositoryMock.Verify(
            x => x.RemoveRangeAsync(It.IsAny<IEnumerable<ContactMedium>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _contactMediumRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ContactMedium>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithContactMedium_ShouldRemoveExistingAndAddNewOnes()
    {
        SetupIndividual(partyId: 5);
        var existing = new List<ContactMedium>
        {
            new() { Id = 10, PartyId = 5, MediumType = ContactMediumType.EmailAddress, Email = "eski@example.com" }
        };
        SetupExistingContactMedia(existing);

        ContactMedium? added = null;
        _contactMediumRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ContactMedium>(), It.IsAny<CancellationToken>()))
            .Callback<ContactMedium, CancellationToken>((m, _) => added = m)
            .Returns(Task.CompletedTask);

        var command = new PatchIndividualCommand
        {
            Id = 1,
            ContactMedium = new List<ContactMediumRequest>
            {
                EmailRequest("yeni@example.com", preferred: true)
            }
        };

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _contactMediumRepositoryMock.Verify(
            x => x.RemoveRangeAsync(
                It.Is<IEnumerable<ContactMedium>>(e => e.SequenceEqual(existing)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        added.Should().NotBeNull();
        added!.PartyId.Should().Be(5);
        added.Email.Should().Be("yeni@example.com");
        added.MediumType.Should().Be(ContactMediumType.EmailAddress);
        added.IsPreferred.Should().BeTrue();
        added.CredentialId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithContactMedium_ShouldOnlyDeleteOwnContactMediaNotCredentialOnes()
    {
        SetupIndividual(partyId: 5);

        // Handler'ın FindAsync'e verdiği predicate'i yakala; gerçek kayıtlar üzerinde dene.
        Expression<Func<ContactMedium, bool>>? predicate = null;
        _contactMediumRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<ContactMedium, bool>>>(),
                It.IsAny<Func<IQueryable<ContactMedium>, IOrderedQueryable<ContactMedium>>?>(),
                It.IsAny<Func<IQueryable<ContactMedium>, IQueryable<ContactMedium>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                Expression<Func<ContactMedium, bool>>,
                Func<IQueryable<ContactMedium>, IOrderedQueryable<ContactMedium>>?,
                Func<IQueryable<ContactMedium>, IQueryable<ContactMedium>>?,
                bool,
                CancellationToken>((p, _, _, _, _) => predicate = p)
            .ReturnsAsync((IReadOnlyList<ContactMedium>)new List<ContactMedium>());

        var command = new PatchIndividualCommand
        {
            Id = 1,
            ContactMedium = new List<ContactMediumRequest> { EmailRequest("yeni@example.com") }
        };

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        predicate.Should().NotBeNull();
        var matches = predicate!.Compile();

        matches(new ContactMedium { PartyId = 5, CredentialId = null }).Should().BeTrue();          // kişinin kendi kaydı
        matches(new ContactMedium { PartyId = 5, CredentialId = Guid.NewGuid() }).Should().BeFalse(); // giriş e-postası/telefonu
        matches(new ContactMedium { PartyId = 6, CredentialId = null }).Should().BeFalse();          // başka kişi
    }

    [Fact]
    public async Task Handle_WithInvalidContactMedium_ShouldThrowAndChangeNothing()
    {
        SetupIndividual(partyId: 5);
        SetupExistingContactMedia(new List<ContactMedium>
        {
            new() { Id = 10, PartyId = 5, MediumType = ContactMediumType.EmailAddress, Email = "eski@example.com" }
        });

        var command = new PatchIndividualCommand
        {
            Id = 1,
            GivenName = "Mehmet",
            ContactMedium = new List<ContactMediumRequest>
            {
                new() { MediumType = "BilinmeyenTur", Characteristic = new Dictionary<string, object>() }
            }
        };

        Func<Task> act = () => _handler.Handle(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
        _contactMediumRepositoryMock.Verify(
            x => x.RemoveRangeAsync(It.IsAny<IEnumerable<ContactMedium>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _individualRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyContactMediumList_ShouldRemoveExistingAndAddNothing()
    {
        SetupIndividual(partyId: 5);
        var existing = new List<ContactMedium>
        {
            new() { Id = 10, PartyId = 5, MediumType = ContactMediumType.EmailAddress, Email = "eski@example.com" }
        };
        SetupExistingContactMedia(existing);

        var command = new PatchIndividualCommand { Id = 1, ContactMedium = new List<ContactMediumRequest>() };

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _contactMediumRepositoryMock.Verify(
            x => x.RemoveRangeAsync(It.IsAny<IEnumerable<ContactMedium>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _contactMediumRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ContactMedium>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────

    private void SetupIndividual(int partyId)
    {
        var individual = new Individual
        {
            Id = 1,
            PartyId = partyId,
            GivenName = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = DateTime.MinValue,
            ValidForEnd = DateTime.MaxValue
        };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(individual);

        _individualRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // Handler FindAsync'i isteğe bağlı parametreleri atlayarak çağırıyor, ama Moq'un expression
    // tree'sinde parametre atlanamaz: hepsi It.IsAny ile verilmeli.
    private void SetupExistingContactMedia(List<ContactMedium> existing)
    {
        _contactMediumRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<ContactMedium, bool>>>(),
                It.IsAny<Func<IQueryable<ContactMedium>, IOrderedQueryable<ContactMedium>>?>(),
                It.IsAny<Func<IQueryable<ContactMedium>, IQueryable<ContactMedium>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ContactMedium>)existing);

        _contactMediumRepositoryMock
            .Setup(x => x.RemoveRangeAsync(It.IsAny<IEnumerable<ContactMedium>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static ContactMediumRequest EmailRequest(string email, bool preferred = false) => new()
    {
        MediumType = "EmailAddress",
        Preferred = preferred,
        Characteristic = new Dictionary<string, object> { ["emailAddress"] = email }
    };
}