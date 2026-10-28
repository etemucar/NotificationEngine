using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Service.Features.Handlers;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Tests.Features.Queries;

public class GetOrganizationListQueryHandlerTests
{
    private readonly Mock<IRepository<Organization, int>>           _organizationRepositoryMock;
    private readonly Mock<IMapper>                                  _mapperMock;
    private readonly Mock<ILogger<GetOrganizationListQueryHandler>> _loggerMock;
    private readonly GetOrganizationListQueryHandler                _handler;

    public GetOrganizationListQueryHandlerTests()
    {
        _organizationRepositoryMock = new Mock<IRepository<Organization, int>>();
        _mapperMock                 = new Mock<IMapper>();
        _loggerMock                 = new Mock<ILogger<GetOrganizationListQueryHandler>>();

        _mapperMock
            .Setup(x => x.Map<OrganizationResponse>(It.IsAny<Organization>()))
            .Returns((Organization o) => new OrganizationResponse
            {
                Id        = o.Id,
                Name      = o.Name,
                TaxNumber = o.TaxNumber,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = o.ValidForStart,
                    EndDateTime   = o.ValidForEnd
                }
            });

        _handler = new GetOrganizationListQueryHandler(
            _organizationRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllOrganizations()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            new() { Id = 1, Name = "Test A.Ş.",   TaxNumber = 1234567890, ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue},
            new() { Id = 2, Name = "Örnek Ltd.",  TaxNumber = 9876543210, ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue}
        };

        _organizationRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<Func<IQueryable<Organization>, IOrderedQueryable<Organization>>>(),
                It.IsAny<Func<IQueryable<Organization>, IQueryable<Organization>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        // Act
        var result = await _handler.Handle(new GetOrganizationListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Test A.Ş.");
    }

    [Fact]
    public async Task Handle_EmptyList_ShouldReturnEmptyCollection()
    {
        // Arrange
        _organizationRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<Organization, bool>>>(),
                It.IsAny<Func<IQueryable<Organization>, IOrderedQueryable<Organization>>>(),
                It.IsAny<Func<IQueryable<Organization>, IQueryable<Organization>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>());

        // Act
        var result = await _handler.Handle(new GetOrganizationListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}
