using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateOrganizationCommandHandlerTests
{
    private readonly Mock<IRepository<Party, int>>                   _partyRepositoryMock;
    private readonly Mock<IUnitOfWork>                               _unitOfWorkMock;
    private readonly Mock<IMapper>                                   _mapperMock;
    private readonly Mock<ILogger<CreateOrganizationCommandHandler>> _loggerMock;
    private readonly CreateOrganizationCommandHandler                _handler;

    public CreateOrganizationCommandHandlerTests()
    {
        _partyRepositoryMock = new Mock<IRepository<Party, int>>();
        _unitOfWorkMock      = new Mock<IUnitOfWork>();
        _mapperMock          = new Mock<IMapper>();
        _loggerMock          = new Mock<ILogger<CreateOrganizationCommandHandler>>();

        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // DÜZELTME: aynı gerekçe — CreateIndividualCommandHandlerTests'e bkz.
        // IRepository<Organization,int> handler'dan tamamen kaldırıldı, FK artık
        // navigation (party.Organization = organization) ile kuruluyor.
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(x => x.Map<OrganizationResponse>(It.IsAny<Organization>()))
            .Returns((Organization o) => new OrganizationResponse
            {
                Id                  = o.Id,
                Name                = o.Name,
                TaxOffice           = o.TaxOffice,
                TaxNumber           = o.TaxNumber,
                IdentityNumber      = o.IdentityNumber,
                TradeName           = o.TradeName,
                TradeRegisterNumber = o.TradeRegisterNumber,
                MersisNo            = o.MersisNo,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = o.ValidForStart,
                    EndDateTime   = o.ValidForEnd
                }
            });

        _handler = new CreateOrganizationCommandHandler(
            _partyRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePartyAndOrganization()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.TaxOffice.Should().Be(command.TaxOffice);
        result.TaxNumber.Should().Be(command.TaxNumber);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallPartyRepositoryOnce()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _partyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldLinkOrganizationToPartyViaNavigation()
    {
        // DEĞİŞTİ: bkz. CreateIndividualCommandHandlerTests'teki aynı gerekçe —
        // FK artık navigation ile kuruluyor, elle .PartyId atanmıyor.
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

        Party? capturedParty = null;
        _partyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((party, _) => capturedParty = party)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedParty.Should().NotBeNull();
        capturedParty!.Organization.Should().NotBeNull();
        capturedParty.Organization!.Name.Should().Be("Test A.Ş.");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveChangesAfterAddingParty()
    {
        // YENİ TEST: regresyon koruması.
        var command = new CreateOrganizationCommand
        {
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890
        };

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
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name          = "Test A.Ş.",
            TaxOffice     = "Kadıköy",
            TaxNumber     = 1234567890,
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
        capturedParty!.Organization!.ValidForStart.Should().Be(DateTime.MinValue);
        capturedParty.Organization.ValidForEnd.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldMapDatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate   = new DateTime(2025, 12, 31);

        var command = new CreateOrganizationCommand
        {
            Name          = "Test A.Ş.",
            TaxOffice     = "Kadıköy",
            TaxNumber     = 1234567890,
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
        capturedParty!.Organization!.ValidForStart.Should().Be(startDate);
        capturedParty.Organization.ValidForEnd.Should().Be(endDate);
    }
}
