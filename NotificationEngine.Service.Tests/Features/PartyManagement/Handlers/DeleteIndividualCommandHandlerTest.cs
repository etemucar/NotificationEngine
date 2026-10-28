using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class DeleteIndividualCommandHandlerTests
{
    private readonly Mock<IRepository<Individual, int>> _individualRepositoryMock;
    private readonly Mock<ILogger<DeleteIndividualCommandHandler>> _loggerMock;
    private readonly DeleteIndividualCommandHandler _handler;

    public DeleteIndividualCommandHandlerTests()
    {
        _individualRepositoryMock = new Mock<IRepository<Individual, int>>();
        _loggerMock               = new Mock<ILogger<DeleteIndividualCommandHandler>>();

        _handler = new DeleteIndividualCommandHandler(
            _individualRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingIndividual_ShouldReturnTrue()
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

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _individualRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteIndividualCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistingIndividual_ShouldReturnFalse()
    {
        // Arrange
        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Individual?)null);

        // Act
        var result = await _handler.Handle(new DeleteIndividualCommand { Id = 99 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingIndividual_ShouldCallRemoveOnce()
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

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _individualRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new DeleteIndividualCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        _individualRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<Individual>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
