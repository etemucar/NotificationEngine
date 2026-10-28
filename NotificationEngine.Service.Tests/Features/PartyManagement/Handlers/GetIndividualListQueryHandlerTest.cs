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

public class GetIndividualListQueryHandlerTests
{
    private readonly Mock<IRepository<Individual, int>>           _individualRepositoryMock;
    private readonly Mock<IMapper>                                _mapperMock;
    private readonly Mock<ILogger<GetIndividualListQueryHandler>> _loggerMock;
    private readonly GetIndividualListQueryHandler                _handler;

    public GetIndividualListQueryHandlerTests()
    {
        _individualRepositoryMock = new Mock<IRepository<Individual, int>>();
        _mapperMock               = new Mock<IMapper>();
        _loggerMock               = new Mock<ILogger<GetIndividualListQueryHandler>>();

        // DÜZELTME: handler artık IMapper alıyor. Gerçek mapping'i taklit ediyoruz.
        _mapperMock
            .Setup(x => x.Map<IndividualResponse>(It.IsAny<Individual>()))
            .Returns((Individual i) => new IndividualResponse
            {
                Id         = i.Id,
                GivenName  = i.GivenName,
                FamilyName = i.FamilyName,
                ValidFor = new TimePeriodResponse
                {
                    StartDateTime = i.ValidForStart,
                    EndDateTime   = i.ValidForEnd
                }
            });

        _handler = new GetIndividualListQueryHandler(
            _individualRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllIndividuals()
    {
        // Arrange
        var individuals = new List<Individual>
        {
            new() { Id = 1, GivenName = "Ahmet",  FamilyName = "Yılmaz", ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue },
            new() { Id = 2, GivenName = "Mehmet", FamilyName = "Demir",  ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue }
        };

        _individualRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<Individual, bool>>>(),
                It.IsAny<Func<IQueryable<Individual>, IOrderedQueryable<Individual>>>(),
                It.IsAny<Func<IQueryable<Individual>, IQueryable<Individual>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(individuals);

        // Act
        var result = await _handler.Handle(new GetIndividualListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().GivenName.Should().Be("Ahmet");
    }

    [Fact]
    public async Task Handle_EmptyList_ShouldReturnEmptyCollection()
    {
        // Arrange
        _individualRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<Individual, bool>>>(),
                It.IsAny<Func<IQueryable<Individual>, IOrderedQueryable<Individual>>>(),
                It.IsAny<Func<IQueryable<Individual>, IQueryable<Individual>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Individual>());

        // Act
        var result = await _handler.Handle(new GetIndividualListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}
