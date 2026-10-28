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

public class CreatePartyRoleCommandHandlerTests
{
    private readonly Mock<IRepository<PartyRole, int>>            _partyRoleRepositoryMock;
    private readonly Mock<IUnitOfWork>                            _unitOfWorkMock;
    private readonly Mock<IMapper>                                _mapperMock;
    private readonly Mock<ILogger<CreatePartyRoleCommandHandler>> _loggerMock;
    private readonly CreatePartyRoleCommandHandler                _handler;

    public CreatePartyRoleCommandHandlerTests()
    {
        _partyRoleRepositoryMock = new Mock<IRepository<PartyRole, int>>();
        _unitOfWorkMock          = new Mock<IUnitOfWork>();
        _mapperMock              = new Mock<IMapper>();
        _loggerMock              = new Mock<ILogger<CreatePartyRoleCommandHandler>>();

        // DÜZELTME: handler artık IUnitOfWork alıyor — partyRole.Id'nin
        // log/response'ta doğru görünmesi için SaveChanges manuel tetikleniyor.
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(x => x.Map<PartyRoleResponse>(It.IsAny<PartyRole>()))
            .Returns((PartyRole pr) => new PartyRoleResponse
            {
                Id              = pr.Id,
                PartyId         = pr.PartyId,
                PartyRoleTypeId = pr.PartyRoleTypeId,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = pr.ValidForStart,
                    EndDateTime   = pr.ValidForEnd
                }
            });

        _handler = new CreatePartyRoleCommandHandler(
            _partyRoleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePartyRoleAndReturnResponse()
    {
        // Arrange
        var command = new CreatePartyRoleCommand
        {
            PartyId         = 1,
            PartyRoleTypeId = 1
        };

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.PartyId.Should().Be(command.PartyId);
        result.PartyRoleTypeId.Should().Be(command.PartyRoleTypeId);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallRepositoryOnce()
    {
        // Arrange
        var command = new CreatePartyRoleCommand
        {
            PartyId         = 1,
            PartyRoleTypeId = 1
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _partyRoleRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveChangesAfterAdding()
    {
        // YENİ TEST: regresyon koruması.
        var command = new CreatePartyRoleCommand { PartyId = 1, PartyRoleTypeId = 1 };

        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutValidFor_ShouldSetMinMaxDateTime()
    {
        // Arrange — ValidFor gönderilmediğinde handler DateTime.MinValue/MaxValue set etmeli
        var command = new CreatePartyRoleCommand
        {
            PartyId         = 1,
            PartyRoleTypeId = 1,
            ValidForStart   = null,
            ValidForEnd     = null
        };

        PartyRole capturedPartyRole = null!;
        _partyRoleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Callback<PartyRole, CancellationToken>((pr, _) => capturedPartyRole = pr)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedPartyRole.ValidForStart.Should().Be(DateTime.MinValue);
        capturedPartyRole.ValidForEnd.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldMapDatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate   = new DateTime(2025, 12, 31);

        var command = new CreatePartyRoleCommand
        {
            PartyId         = 1,
            PartyRoleTypeId = 1,
            ValidForStart   = startDate,
            ValidForEnd     = endDate
        };

        PartyRole capturedPartyRole = null!;
        _partyRoleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Callback<PartyRole, CancellationToken>((pr, _) => capturedPartyRole = pr)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedPartyRole.ValidForStart.Should().Be(startDate);
        capturedPartyRole.ValidForEnd.Should().Be(endDate);
    }
}
