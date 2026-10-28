using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Tests.Features.Queries;

public class GetOrganizationQueryHandlerTests
{
    private readonly Mock<IRepository<Organization, int>>      _organizationRepositoryMock;
    private readonly Mock<IMapper>                             _mapperMock;
    private readonly Mock<ILogger<GetOrganizationQueryHandler>> _loggerMock;
    private readonly GetOrganizationQueryHandler                _handler;

    public GetOrganizationQueryHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IRepository<Organization, int>>();
        _mapperMock                 = new Mock<IMapper>();
        _loggerMock                 = new Mock<ILogger<GetOrganizationQueryHandler>>();

        _mapperMock
            .Setup(x => x.Map<OrganizationResponse>(It.IsAny<Organization>()))
            .Returns((Organization o) => new OrganizationResponse
            {
                Id        = o.Id,
                Name      = o.Name,
                TaxOffice = o.TaxOffice,
                TaxNumber = o.TaxNumber,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = o.ValidForStart,
                    EndDateTime   = o.ValidForEnd
                }
            });

        _handler = new GetOrganizationQueryHandler(
            _organizationRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingOrganization_ShouldReturnResponse()
    {
        // Arrange
        var existing = new Organization
        {
            Id        = 1,
            Name      = "Test A.Ş.",
            TaxOffice = "Kadıköy",
            TaxNumber = 1234567890,
            ValidForStart = DateTime.MinValue,
            ValidForEnd   = DateTime.MaxValue
        };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetOrganizationQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test A.Ş.");
        result.TaxOffice.Should().Be("Kadıköy");
        result.TaxNumber.Should().Be(1234567890);
    }

    [Fact]
    public async Task Handle_NonExistingOrganization_ShouldThrowNotFoundException()
    {
        // DEĞİŞTİ: handler artık null dönmüyor, NotFoundException fırlatıyor.
        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var act = async () => await _handler.Handle(
            new GetOrganizationQuery { Id = 99 }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldMapValidForCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 1);
        var end   = new DateTime(2025, 12, 31);

        var existing = new Organization
        {
            Id        = 1,
            Name      = "Test A.Ş.",
            TaxNumber = 1234567890,
            ValidForStart = start,
            ValidForEnd   = end
        };

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetOrganizationQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.ValidFor.StartDateTime.Should().Be(start);
        result.ValidFor.EndDateTime.Should().Be(end);
    }
}
