using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Requests;
using AutoMapper;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class PatchOrganizationCommandHandlerTests
{
    private readonly Mock<IRepository<Organization, int>> _organizationRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<PatchOrganizationCommandHandler>> _loggerMock;
    private readonly PatchOrganizationCommandHandler _handler;

    public PatchOrganizationCommandHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IRepository<Organization, int>>();
        _mapperMock                  = new Mock<IMapper>();
        _loggerMock                 = new Mock<ILogger<PatchOrganizationCommandHandler>>();

        // Diğer Patch*CommandHandlerTests sınıflarında olduğu gibi — bu mapping
        // eksikti, bu yüzden result her zaman null dönüyordu (Moq, kurulmamış
        // bir Map<> çağrısı için default(T) döner).
        _mapperMock
            .Setup(x => x.Map<OrganizationResponse>(It.IsAny<Organization>()))
            .Returns((Organization src) => new OrganizationResponse
            {
                Id        = src.Id,
                Name      = src.Name,
                TaxOffice = src.TaxOffice,
                ValidFor  = new TimePeriodResponse
                {
                    StartDateTime = src.ValidForStart,
                    EndDateTime   = src.ValidForEnd
                }
            });

        _handler = new PatchOrganizationCommandHandler(
            _organizationRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingOrganization_ShouldUpdateAndReturnResponse()
    {
        // Arrange
        var existing = new Organization
        {
            Id        = 1,
            Name      = "Eski A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890,
            ValidForStart = DateTime.MinValue, 
            ValidForEnd = DateTime.MaxValue            
        };

        var command = new PatchOrganizationCommand { Id = 1, Name = "Yeni A.Ş." };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _organizationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Yeni A.Ş.");
        result.TaxOffice.Should().Be("Kadıköy"); // değişmemeli
    }

    [Fact]
    public async Task Handle_NonExistingOrganization_ShouldThrowNotFoundException()
    {
        // Arrange — handler artık null dönmüyor, NotFoundException fırlatıyor
        // (bkz. PatchOrganizationCommandHandler.cs satır 33)
        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        // Act
        Func<Task> act = () => _handler.Handle(
            new PatchOrganizationCommand { Id = 99 },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldUpdateDates()
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

        var newStart = new DateTime(2024, 1, 1);
        var newEnd   = new DateTime(2025, 12, 31);

        var command = new PatchOrganizationCommand
        {
            Id       = 1,
            ValidForStart = newStart, 
            ValidForEnd = newEnd
        };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _organizationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
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
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new PatchOrganizationCommand { Id = 1, Name = "Yeni A.Ş." }, TestContext.Current.CancellationToken);

        // Assert
        _organizationRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
