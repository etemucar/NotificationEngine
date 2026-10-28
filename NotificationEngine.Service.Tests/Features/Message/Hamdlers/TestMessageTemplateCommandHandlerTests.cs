using FluentAssertions;
using MediatR;
using Moq;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class TestMessageTemplateCommandHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TestMessageTemplateCommandHandler _handler;
    private readonly SendNowResponse _response = new(true, default, new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc));
    private SendNowCommand? _sent;

    public TestMessageTemplateCommandHandlerTests()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SendNowCommand>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest<SendNowResponse> command, CancellationToken _) =>
            {
                _sent = (SendNowCommand)command;
                return Task.FromResult(_response);
            });

        _handler = new TestMessageTemplateCommandHandler(_repo.Object, _mediatorMock.Object);
    }

    private static TestMessageTemplateCommand Command(
        int id = 1, int? organizationId = 7, string recipient = "+905551112233",
        Dictionary<string, string>? parameters = null) =>
        new(id, organizationId, recipient, parameters);

    [Fact]
    public async Task Handle_SmsTemplate_RendersAndSendsViaSendNow()
    {
        _repo.Add(1, 7, channel: Channel.Sms, body: "Kodunuz: {{code}}");

        var result = await _handler.Handle(
            Command(recipient: "  +905551112233  ", parameters: new() { ["code"] = "123456" }),
            TestContext.Current.CancellationToken);

        _sent.Should().NotBeNull();
        _sent!.Channel.Should().Be(Channel.Sms);
        _sent.Recipient.Should().Be("+905551112233");        // boşluklar kırpılır
        _sent.Content.Should().Be("Kodunuz: 123456");
        _sent.Subject.Should().BeNull();                      // SMS'e işaret eklenmez (bölünmesin)
        _sent.TemplateCode.Should().BeNull();                 // şablon zaten işlendi
        _sent.Parameters.Should().BeNull();
        result.Should().BeSameAs(_response);
    }

    [Fact]
    public async Task Handle_EmailTemplate_PrefixesSubjectWithTest()
    {
        _repo.Add(1, 7, channel: Channel.Email, subject: "Merhaba {{name}}", body: "Hoş geldin {{name}}");

        await _handler.Handle(
            Command(recipient: "ali@example.com", parameters: new() { ["name"] = "Ali" }),
            TestContext.Current.CancellationToken);

        _sent!.Channel.Should().Be(Channel.Email);
        _sent.Subject.Should().Be("[TEST] Merhaba Ali");
        _sent.Content.Should().Be("Hoş geldin Ali");
    }

    [Fact]
    public async Task Handle_PassiveTemplate_CanStillBeTested()
    {
        _repo.Add(1, 7, status: GeneralStatus.Passive, body: "Merhaba");

        await _handler.Handle(Command(), TestContext.Current.CancellationToken);

        _sent.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SharedTemplate_CanBeTestedByAnyOrganization()
    {
        _repo.Add(1, organizationId: null, body: "Merhaba");

        await _handler.Handle(Command(organizationId: 7), TestContext.Current.CancellationToken);

        _sent.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_OtherOrganizationsTemplate_ThrowsNotFoundAndSendsNothing()
    {
        _repo.Add(1, organizationId: 99, body: "Merhaba");

        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SendNowCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingTemplate_ThrowsNotFound()
    {
        var act = () => _handler.Handle(Command(id: 42), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*42*");
    }

    [Fact]
    public async Task Handle_MissingParameter_ThrowsInvalidInputAndSendsNothing()
    {
        _repo.Add(1, 7, body: "Kodunuz: {{code}}");

        var act = () => _handler.Handle(Command(parameters: new()), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SendNowCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullParametersOnTemplateWithoutPlaceholders_Sends()
    {
        _repo.Add(1, 7, body: "Sabit metin");

        await _handler.Handle(Command(parameters: null), TestContext.Current.CancellationToken);

        _sent!.Content.Should().Be("Sabit metin");
    }
}