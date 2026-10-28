using FluentAssertions;
using Moq;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class UpdateMessageTemplateCommandHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateMessageTemplateCommandHandler _handler;

    public UpdateMessageTemplateCommandHandlerTests()
    {
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateMessageTemplateCommandHandler(_repo.Object, _uowMock.Object, TestMapper.Create());
    }

    [Fact]
    public async Task Handle_OwnTemplate_UpdatesSubjectAndBodyAndSaves()
    {
        var template = _repo.Add(1, 7, channel: Channel.Email, subject: "Eski konu", body: "eski");

        var response = await _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, "Yeni konu", "Yeni gövde"),
            TestContext.Current.CancellationToken);

        template.Subject.Should().Be("Yeni konu");
        template.Body.Should().Be("Yeni gövde");
        _repo.Updated.Should().ContainSingle();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        response.Body.Should().Be("Yeni gövde");
    }

    [Fact]
    public async Task Handle_KeyFieldsAreNotChanged()
    {
        var template = _repo.Add(1, 7, code: "OTP", channel: Channel.Sms, culture: "tr-TR");

        await _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, null, "Yeni"), TestContext.Current.CancellationToken);

        template.Code.Should().Be("OTP");
        template.Channel.Should().Be(Channel.Sms);
        template.Culture.Should().Be("tr-TR");
        template.OrganizationId.Should().Be(7);
    }

    [Fact]
    public async Task Handle_MissingTemplate_ThrowsNotFound()
    {
        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(42, 7, null, "Yeni"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*42*");
    }

    [Fact]
    public async Task Handle_OtherOrganizationsTemplate_ThrowsNotFoundAndChangesNothing()
    {
        var template = _repo.Add(1, organizationId: 99, body: "eski");

        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, null, "Yeni"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
        template.Body.Should().Be("eski");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SharedTemplateByOrganization_ThrowsNotFound()
    {
        _repo.Add(1, organizationId: null);   // ortak şablonu kuruluş değiştiremez

        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, null, "Yeni"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SharedTemplateBySiteAdmin_Updates()
    {
        var template = _repo.Add(1, organizationId: null, body: "eski");

        await _handler.Handle(
            new UpdateMessageTemplateCommand(1, null, null, "Yeni"), TestContext.Current.CancellationToken);

        template.Body.Should().Be("Yeni");
    }

    [Fact]
    public async Task Handle_OrganizationTemplateBySiteAdmin_ThrowsNotFound()
    {
        _repo.Add(1, organizationId: 7);   // SiteAdmin (OrganizationId boş) yalnızca ortak şablonları yönetir

        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(1, null, null, "Yeni"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_EmailWithoutSubject_ThrowsInvalidInputAndChangesNothing()
    {
        var template = _repo.Add(1, 7, channel: Channel.Email, subject: "Konu", body: "eski");

        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, "  ", "Yeni"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>().WithMessage("*konu*");
        template.Subject.Should().Be("Konu");
        template.Body.Should().Be("eski");
    }

    [Fact]
    public async Task Handle_SmsBodyLongerThan1000_ThrowsInvalidInput()
    {
        _repo.Add(1, 7, channel: Channel.Sms);

        var act = () => _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, null, new string('a', 1_001)),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Fact]
    public async Task Handle_EmailBodyLongerThan1000_IsAllowed()
    {
        var template = _repo.Add(1, 7, channel: Channel.Email, subject: "Konu");

        await _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, "Konu", new string('a', 5_000)),
            TestContext.Current.CancellationToken);

        template.Body.Length.Should().Be(5_000);
    }

    [Fact]
    public async Task Handle_BlankSubjectOnSms_IsStoredAsNull()
    {
        var template = _repo.Add(1, 7, channel: Channel.Sms, subject: "eski konu");

        await _handler.Handle(
            new UpdateMessageTemplateCommand(1, 7, " ", "Yeni"), TestContext.Current.CancellationToken);

        template.Subject.Should().BeNull();
    }
}