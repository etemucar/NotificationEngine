using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class DeleteOrganizationCommandHandlerTests
{
    private readonly Mock<IRepository<Organization, int>> _organizationRepositoryMock;
    private readonly Mock<ILogger<DeleteOrganizationCommandHandler>> _loggerMock;
    private readonly DeleteOrganizationCommandHandler _handler;

    public DeleteOrganizationCommandHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IRepository<Organization, int>>();
        _loggerMock                 = new Mock<ILogger<DeleteOrganizationCommandHandler>>();

        _handler = new DeleteOrganizationCommandHandler(
            _organizationRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingOrganization_ShouldReturnTrue()
    {
        // Arrange
        var existing = new Organization
        {
            Id        = 1,
            Name      = "Test A.Ş.",
            TaxNumber = 1234567890,
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _organizationRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteOrganizationCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistingOrganization_ShouldReturnFalse()
    {
        // Arrange
        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _handler.Handle(new DeleteOrganizationCommand { Id = 99 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingOrganization_ShouldCallRemoveOnce()
    {
        // Arrange
        var existing = new Organization
        {
            Id        = 1,
            Name      = "Test A.Ş.",
            TaxNumber = 1234567890,
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue
        };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _organizationRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new DeleteOrganizationCommand { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        _organizationRepositoryMock.Verify(
            x => x.RemoveAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
