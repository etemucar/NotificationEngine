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

public class GetPartyRoleQueryHandlerTests
{
    private readonly Mock<IRepository<PartyRole, int>>          _partyRoleRepositoryMock;
    private readonly Mock<IMapper>                              _mapperMock;
    private readonly Mock<ILogger<GetPartyRoleQueryHandler>>    _loggerMock;
    private readonly GetPartyRoleQueryHandler                   _handler;

    public GetPartyRoleQueryHandlerTests()
    {
        _partyRoleRepositoryMock = new Mock<IRepository<PartyRole, int>>();
        _mapperMock              = new Mock<IMapper>();
        _loggerMock              = new Mock<ILogger<GetPartyRoleQueryHandler>>();

        // Handler'ı AutoMapper profilinden izole tutmak için manuel mapping — sadece
        // testlerin assert ettiği alanları taşıyor.
        _mapperMock
            .Setup(x => x.Map<PartyRoleResponse>(It.IsAny<PartyRole>()))
            .Returns((PartyRole src) => new PartyRoleResponse
            {
                Id              = src.Id,
                PartyId         = src.PartyId,
                PartyRoleTypeId = src.PartyRoleTypeId,
                ValidFor        = new TimePeriodResponse
                {
                    StartDateTime = src.ValidForStart,
                    EndDateTime   = src.ValidForEnd
                }
            });

        _handler = new GetPartyRoleQueryHandler(
            _partyRoleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPartyRole_ShouldReturnResponse()
    {
        // Arrange
        var existing = new PartyRole
        {
            Id              = 1,
            PartyId         = 10,
            PartyRoleTypeId = 1,
            ValidForStart   = DateTime.MinValue,
            ValidForEnd     = DateTime.MaxValue
        };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetPartyRoleQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.PartyId.Should().Be(10);
        result.PartyRoleTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NonExistingPartyRole_ShouldThrowNotFoundException()
    {
        // Arrange — handler artık null dönmüyor, NotFoundException fırlatıyor
        // (bkz. GetPartyRoleQueryHandler.cs satır 33)
        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyRole?)null);

        // Act
        Func<Task> act = () => _handler.Handle(
            new GetPartyRoleQuery { Id = 99 },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_ShouldMapValidForCorrectly()
    {
        // Arrange — mock'taki ValidFor değerleriyle assertion tutarlı olmalı
        var start = new DateTime(2024, 1, 1);
        var end   = new DateTime(2025, 12, 31);

        var existing = new PartyRole
        {
            Id              = 1,
            PartyId         = 10,
            PartyRoleTypeId = 1,
            ValidForStart   = start,   // assertion'daki değerle aynı
            ValidForEnd     = end
        };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(new GetPartyRoleQuery { Id = 1 }, TestContext.Current.CancellationToken);

        // Assert
        result.ValidFor.StartDateTime.Should().Be(start);
        result.ValidFor.EndDateTime.Should().Be(end);
    }
}
