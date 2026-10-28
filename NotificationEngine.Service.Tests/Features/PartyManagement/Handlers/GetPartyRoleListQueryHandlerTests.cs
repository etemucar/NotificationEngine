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

public class GetPartyRoleListQueryHandlerTests
{
    private readonly Mock<IRepository<PartyRole, int>>           _partyRoleRepositoryMock;
    private readonly Mock<IMapper>                               _mapperMock;
    private readonly Mock<ILogger<GetPartyRoleListQueryHandler>> _loggerMock;
    private readonly GetPartyRoleListQueryHandler                _handler;

    public GetPartyRoleListQueryHandlerTests()
    {
        _partyRoleRepositoryMock = new Mock<IRepository<PartyRole, int>>();
        _mapperMock              = new Mock<IMapper>();
        _loggerMock              = new Mock<ILogger<GetPartyRoleListQueryHandler>>();

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

        _handler = new GetPartyRoleListQueryHandler(
            _partyRoleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllPartyRoles()
    {
        // Arrange
        var partyRoles = new List<PartyRole>
        {
            new() { Id = 1, PartyId = 10, PartyRoleTypeId = 1,  ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue },
            new() { Id = 2, PartyId = 20, PartyRoleTypeId = 2, ValidForStart = DateTime.MinValue, ValidForEnd = DateTime.MaxValue }
        };

        _partyRoleRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<PartyRole, bool>>>(),
                It.IsAny<Func<IQueryable<PartyRole>, IOrderedQueryable<PartyRole>>>(),
                It.IsAny<Func<IQueryable<PartyRole>, IQueryable<PartyRole>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(partyRoles);

        // Act
        var result = await _handler.Handle(new GetPartyRoleListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().PartyRoleTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyList_ShouldReturnEmptyCollection()
    {
        // Arrange
        _partyRoleRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<PartyRole, bool>>>(),
                It.IsAny<Func<IQueryable<PartyRole>, IOrderedQueryable<PartyRole>>>(),
                It.IsAny<Func<IQueryable<PartyRole>, IQueryable<PartyRole>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyRole>());

        // Act
        var result = await _handler.Handle(new GetPartyRoleListQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}
