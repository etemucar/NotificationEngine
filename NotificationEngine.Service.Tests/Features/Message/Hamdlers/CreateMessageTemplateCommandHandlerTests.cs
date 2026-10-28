using FluentAssertions;
using Moq;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateMessageTemplateCommandHandlerTests
{
    private readonly TemplateRepositoryFake _repo = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateMessageTemplateCommandHandler _handler;

    public CreateMessageTemplateCommandHandlerTests()
    {
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateMessageTemplateCommandHandler(_repo.Object, _uowMock.Object, TestMapper.Create());
    }

    private static CreateMessageTemplateCommand Command(
        int? organizationId = 7, string code = "OTP", Channel channel = Channel.Sms,
        string culture = "tr-TR", string? subject = null, string body = "Kodunuz: {{code}}") =>
        new(organizationId, code, channel, culture, subject, body);

    [Fact]
    public async Task Handle_NewTemplate_IsStoredActiveAndSaved()
    {
        var response = await _handler.Handle(Command(code: "  OTP  "), TestContext.Current.CancellationToken);

        var added = _repo.Added.Should().ContainSingle().Subject;
        added.OrganizationId.Should().Be(7);
        added.Code.Should().Be("OTP");                       // boşluklar kırpılır
        added.Status.Should().Be(GeneralStatus.Active);      // ModelBase varsayılanı Created: açıkça Active
        added.Body.Should().Be("Kodunuz: {{code}}");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        response.Code.Should().Be("OTP");
        response.Status.Should().Be(GeneralStatus.Active);
    }

    [Fact]
    public async Task Handle_BlankSubject_IsStoredAsNull()
    {
        await _handler.Handle(Command(subject: "  "), TestContext.Current.CancellationToken);

        _repo.Added.Single().Subject.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SameKeyForSameOrganization_ThrowsConflictAndAddsNothing()
    {
        _repo.Add(1, organizationId: 7);

        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>();
        _repo.Added.Should().BeEmpty();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameKeyAsPassiveTemplate_StillThrowsConflict()
    {
        _repo.Add(1, organizationId: 7, status: GeneralStatus.Passive);

        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*aktif*");
    }

    [Fact]
    public async Task Handle_SameCodeForDifferentOrganization_IsAllowed()
    {
        _repo.Add(1, organizationId: 99);

        await _handler.Handle(Command(organizationId: 7), TestContext.Current.CancellationToken);

        _repo.Added.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_SameCodeAsSharedTemplate_IsAllowed()
    {
        _repo.Add(1, organizationId: null);   // ortak şablon

        await _handler.Handle(Command(organizationId: 7), TestContext.Current.CancellationToken);

        _repo.Added.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_SameCodeDifferentChannelOrCulture_IsAllowed()
    {
        _repo.Add(1, organizationId: 7, channel: Channel.Sms, culture: "tr-TR");

        await _handler.Handle(
            Command(channel: Channel.Email, subject: "Konu"), TestContext.Current.CancellationToken);
        await _handler.Handle(
            Command(culture: "en-US"), TestContext.Current.CancellationToken);

        _repo.Added.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ConcurrentDuplicateOnSave_ThrowsConflict()
    {
        // Kontrol sırasında yoktu; kaydederken başka bir istek aynı anahtarı yazdı (benzersiz index reddeder).
        var winner = new MessageTemplate
        {
            Id = 5, OrganizationId = 7, Code = "OTP", Channel = Channel.Sms, Culture = "tr-TR", Body = "x"
        };
        _uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => _repo.Items.Add(winner))
            .ThrowsAsync(new InvalidOperationException("duplicate key"));

        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SaveFailsWithoutAnyWinner_RethrowsOriginalException()
    {
        _uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bağlantı koptu"));

        var act = () => _handler.Handle(Command(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bağlantı koptu");
    }
}