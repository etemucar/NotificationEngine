using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class DeletePartyRoleCommandHandlerTests
{
    private readonly Mock<IRepository<PartyRole, int>> _partyRoleRepositoryMock;
    private readonly Mock<ILogger<DeletePartyRoleCommandHandler>> _loggerMock;
    private readonly DeletePartyRoleCommandHandler _handler;

    public DeletePartyRoleCommandHandlerTests()
    {
        _partyRoleRepositoryMock = new Mock<IRepository<PartyRole, int>>();
        _loggerMock              = new Mock<ILogger<DeletePartyRoleCommandHandler>>();

        _handler = new DeletePartyRoleCommandHandler(
            _partyRoleRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPartyRole_ShouldReturnTrue()
    {
        // Arrange
        var existing = new PartyRole
        {
            Id              = 1,
            PartyId         = 10,
            PartyRoleTypeId = 1,
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _partyRoleRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeletePartyRoleCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistingPartyRole_ShouldReturnFalse()
    {
        // Arrange
        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyRole?)null);

        // Act
        var result = await _handler.Handle(new DeletePartyRoleCommand { Id = 99 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingPartyRole_ShouldCallRemoveOnce()
    {
        // Arrange
        var existing = new PartyRole
        {
            Id              = 1,
            PartyId         = 10,
            PartyRoleTypeId = 1,
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _partyRoleRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new DeletePartyRoleCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        _partyRoleRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
