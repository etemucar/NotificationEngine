using Moq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class GetIndividualQueryHandlerTests
{
    private readonly Mock<IRepository<Individual, int>>         _individualRepositoryMock;
    private readonly Mock<IMapper>                              _mapperMock;
    private readonly Mock<ILogger<GetIndividualQueryHandler>>   _loggerMock;
    private readonly GetIndividualQueryHandler                  _handler;

    public GetIndividualQueryHandlerTests()
    {
        _individualRepositoryMock = new Mock<IRepository<Individual, int>>();
        _mapperMock               = new Mock<IMapper>();
        _loggerMock               = new Mock<ILogger<GetIndividualQueryHandler>>();

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

        _handler = new GetIndividualQueryHandler(
            _individualRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingIndividual_ShouldReturnResponse()
    {
        // Arrange
        var existing = new Individual
        {
            Id         = 1,
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = DateTime.MinValue,
            ValidForEnd   = DateTime.MaxValue
        };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetIndividualQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.GivenName.Should().Be("Ahmet");
        result.FamilyName.Should().Be("Yılmaz");
    }

    [Fact]
    public async Task Handle_NonExistingIndividual_ShouldThrowNotFoundException()
    {
        // Arrange — GetIndividualQueryHandler NotFoundException fırlatıyor (null dönmüyor)
        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Individual?)null);

        // Act
        var act = async () => await _handler.Handle(new GetIndividualQuery { Id = 99 }, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldMapValidForCorrectly()
    {
        // Arrange
        var start = new DateTime(2024, 1, 1);
        var end   = new DateTime(2025, 12, 31);

        var existing = new Individual
        {
            Id         = 1,
            GivenName  = "Ahmet",
            FamilyName = "Yılmaz",
            ValidForStart = start,
            ValidForEnd   = end
        };

        _individualRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetIndividualQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.ValidFor.StartDateTime.Should().Be(start);
        result.ValidFor.EndDateTime.Should().Be(end);
    }
}
