using Moq;
using AutoMapper;
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

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class PatchPartyRoleCommandHandlerTests
{
    private readonly Mock<IRepository<PartyRole, int>>             _partyRoleRepositoryMock;
    private readonly Mock<IMapper>                                 _mapperMock;
    private readonly Mock<ILogger<PatchPartyRoleCommandHandler>>   _loggerMock;
    private readonly PatchPartyRoleCommandHandler                  _handler;

    public PatchPartyRoleCommandHandlerTests()
    {
        _partyRoleRepositoryMock = new Mock<IRepository<PartyRole, int>>();
        _mapperMock              = new Mock<IMapper>();
        _loggerMock              = new Mock<ILogger<PatchPartyRoleCommandHandler>>();

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

        _handler = new PatchPartyRoleCommandHandler(
            _partyRoleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPartyRole_ShouldUpdateAndReturnResponse()
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

        var command = new PatchPartyRoleCommand { Id = 1, PartyRoleTypeId = 2 };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _partyRoleRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.PartyRoleTypeId.Should().Be(2);
        result.PartyId.Should().Be(10);
    }

    [Fact]
    public async Task Handle_NonExistingPartyRole_ShouldThrowNotFoundException()
    {
        // Arrange — handler artık null dönmüyor, NotFoundException fırlatıyor
        // (bkz. PatchPartyRoleCommandHandler.cs satır 33)
        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyRole?)null);

        // Act
        Func<Task> act = () => _handler.Handle(
            new PatchPartyRoleCommand { Id = 99 },
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_WithValidFor_ShouldUpdateDates()
    {
        // Arrange
        var newStart = new DateTime(2024, 1, 1);
        var newEnd   = new DateTime(2025, 12, 31);

        var existing = new PartyRole
        {
            Id              = 1,
            PartyId         = 10,
            PartyRoleTypeId = 1,
            ValidForStart   = DateTime.MinValue,
            ValidForEnd     = DateTime.MaxValue
        };

        // Command'da güncellenecek tarihler — assertion'la tutarlı
        var command = new PatchPartyRoleCommand
        {
            Id            = 1,
            ValidForStart = newStart,
            ValidForEnd   = newEnd
        };

        _partyRoleRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _partyRoleRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ValidFor?.StartDateTime.Should().Be(newStart);
        result.ValidFor?.EndDateTime.Should().Be(newEnd);
    }

    [Fact]
    public async Task Handle_ShouldCallUpdateRepositoryOnce()
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

        _partyRoleRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            new PatchPartyRoleCommand { Id = 1, PartyRoleTypeId = 2 },
            TestContext.Current.CancellationToken);

        // Assert
        _partyRoleRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<PartyRole>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
