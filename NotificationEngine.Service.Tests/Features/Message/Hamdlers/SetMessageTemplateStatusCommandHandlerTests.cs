using FluentAssertions;
using Moq;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class SetMessageTemplateStatusCommandHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly SetMessageTemplateStatusCommandHandler _handler;

    public SetMessageTemplateStatusCommandHandlerTests()
    {
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new SetMessageTemplateStatusCommandHandler(_repo.Object, _uowMock.Object, TestMapper.Create());
    }

    [Fact]
    public async Task Handle_Deactivate_SetsPassiveAndSaves()
    {
        var template = _repo.Add(1, 7, status: GeneralStatus.Active);

        var response = await _handler.Handle(
            new SetMessageTemplateStatusCommand(1, 7, Active: false), TestContext.Current.CancellationToken);

        template.Status.Should().Be(GeneralStatus.Passive);
        response.Status.Should().Be(GeneralStatus.Passive);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Activate_SetsActive()
    {
        var template = _repo.Add(1, 7, status: GeneralStatus.Passive);

        await _handler.Handle(
            new SetMessageTemplateStatusCommand(1, 7, Active: true), TestContext.Current.CancellationToken);

        template.Status.Should().Be(GeneralStatus.Active);
    }

    [Fact]
    public async Task Handle_OtherOrganizationsTemplate_ThrowsNotFoundAndChangesNothing()
    {
        var template = _repo.Add(1, organizationId: 99, status: GeneralStatus.Active);

        var act = () => _handler.Handle(
            new SetMessageTemplateStatusCommand(1, 7, Active: false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
        template.Status.Should().Be(GeneralStatus.Active);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingTemplate_ThrowsNotFound()
    {
        var act = () => _handler.Handle(
            new SetMessageTemplateStatusCommand(42, 7, Active: false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*42*");
    }
}