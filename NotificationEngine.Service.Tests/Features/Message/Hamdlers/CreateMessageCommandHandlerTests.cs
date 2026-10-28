using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using Moq;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class CreateMessageCommandHandlerTests
{
    private const int OrganizationId = 7;
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRepository<OutboxMessage, Guid>> _messagesMock = new();
    private readonly Mock<IRepository<MessageTemplate, int>> _templatesMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly CreateMessageCommandHandler _handler;

    // Repository'lerin yerine geçen bellek içi depolar: mock'a verilen predicate/orderBy gerçekten çalıştırılır.
    private readonly List<OutboxMessage> _storedMessages = new();
    private readonly List<MessageTemplate> _templates = new();
    private OutboxMessage? _added;

    public CreateMessageCommandHandlerTests()
    {
        _currentUserMock
            .Setup(x => x.GetOrganizationIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationId);

        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _messagesMock
            .Setup(x => x.FindOneAsync(
                It.IsAny<Expression<Func<OutboxMessage, bool>>>(),
                It.IsAny<Func<IQueryable<OutboxMessage>, IQueryable<OutboxMessage>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<OutboxMessage, bool>> predicate,
                      Func<IQueryable<OutboxMessage>, IQueryable<OutboxMessage>>? _,
                      bool _, CancellationToken _) =>
                Task.FromResult(_storedMessages.AsQueryable().FirstOrDefault(predicate)));

        _messagesMock
            .Setup(x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessage, CancellationToken>((m, _) => _added = m)
            .Returns(Task.CompletedTask);

        _templatesMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<MessageTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<MessageTemplate>, IOrderedQueryable<MessageTemplate>>?>(),
                It.IsAny<Func<IQueryable<MessageTemplate>, IQueryable<MessageTemplate>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<MessageTemplate, bool>> predicate,
                      Func<IQueryable<MessageTemplate>, IOrderedQueryable<MessageTemplate>>? orderBy,
                      Func<IQueryable<MessageTemplate>, IQueryable<MessageTemplate>>? _,
                      bool _, CancellationToken _) =>
            {
                IQueryable<MessageTemplate> query = _templates.AsQueryable().Where(predicate);
                if (orderBy is not null) query = orderBy(query);
                return Task.FromResult<IReadOnlyList<MessageTemplate>>(query.ToList());
            });

        _mapperMock
            .Setup(x => x.Map<MessageResponse>(It.IsAny<OutboxMessage>()))
            .Returns((OutboxMessage m) => new MessageResponse
            {
                Id = m.Id,
                OrganizationId = m.OrganizationId,
                Channel = m.Channel,
                Recipient = m.Recipient,
                Subject = m.Subject,
                Body = m.Body,
                TemplateCode = m.TemplateCode,
                DeliveryStatus = m.DeliveryStatus,
                NextAttemptAt = m.NextAttemptAt
            });

        _handler = new CreateMessageCommandHandler(
            _messagesMock.Object, _templatesMock.Object, _currentUserMock.Object,
            _uowMock.Object, _mapperMock.Object, new FixedTimeProvider(Now));
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────

    private static CreateMessageCommand Command(
        Channel channel = Channel.Sms,
        string recipient = "+905551112233",
        string? subject = null,
        string? content = "Kodunuz: 1234",
        string? templateCode = null,
        Dictionary<string, string>? parameters = null,
        DateTimeOffset? scheduled = null,
        string key = "key-1") =>
        new(channel, recipient, subject, content, templateCode, parameters,
            MessagePriority.Normal, scheduled, null, key);

    private static MessageTemplate Template(
        int? organizationId, string body, Channel channel = Channel.Sms,
        string? subject = null, GeneralStatus status = GeneralStatus.Active) => new()
    {
        Id = Random.Shared.Next(1, 10_000),
        OrganizationId = organizationId,
        Code = "OTP",
        Channel = channel,
        Culture = "tr-TR",
        Subject = subject,
        Body = body,
        Status = status
    };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    // ── Content modu ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ContentMode_StoresPendingMessageAsGiven()
    {
        var response = await _handler.Handle(Command(), TestContext.Current.CancellationToken);

        _added.Should().NotBeNull();
        _added!.Id.Should().NotBe(Guid.Empty);
        _added.OrganizationId.Should().Be(OrganizationId);
        _added.Body.Should().Be("Kodunuz: 1234");
        _added.TemplateCode.Should().BeNull();
        _added.DeliveryStatus.Should().Be(MessageStatus.Pending);
        _added.IdempotencyKey.Should().Be("key-1");
        _added.NextAttemptAt.Should().Be(Now);
        response.Id.Should().Be(_added.Id);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory]
    [InlineData("PASSWORD_RESET")]
    [InlineData("OTP_LOGIN")]
    
    public async Task Handle_SendNowOnlyTemplateCode_ThrowsInvalidInputAndStoresNothing(string code)
    {
        var act = () => _handler.Handle(
            Command(content: null, templateCode: code, parameters: new() { ["code"] = "1" }),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>().WithMessage("*anında gönderim*");
        _messagesMock.Verify(
            x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SendNowOnlyTemplateCodeWithContent_IsNotAffected()
    {
        // Hazır içerik verilirse şablon kodu kullanılmaz; kural yalnızca şablondan üretilen gövdeyi kapsar.
        await _handler.Handle(
            Command(content: "Merhaba", templateCode: "PASSWORD_RESET"),
            TestContext.Current.CancellationToken);

        _added.Should().NotBeNull();
        _added!.TemplateCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ScheduledSendTime_IsStoredInUtc()
    {
        var scheduled = new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.FromHours(3));

        await _handler.Handle(Command(scheduled: scheduled), TestContext.Current.CancellationToken);

        _added!.NextAttemptAt.Should().Be(new DateTime(2026, 10, 8, 6, 0, 0, DateTimeKind.Utc));
    }

    // ── Şablon modu ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TemplateMode_RendersBodyAndKeepsTemplateCode()
    {
        _templates.Add(Template(null, "Kodunuz: {{code}}"));

        await _handler.Handle(
            Command(content: null, templateCode: "OTP", parameters: new() { ["code"] = "482913" }),
            TestContext.Current.CancellationToken);

        _added!.Body.Should().Be("Kodunuz: 482913");
        _added.TemplateCode.Should().Be("OTP");
    }

    [Fact]
    public async Task Handle_OrganizationSpecificTemplate_WinsOverSharedOne()
    {
        _templates.Add(Template(null, "Genel {{code}}"));
        _templates.Add(Template(OrganizationId, "Özel {{code}}"));

        await _handler.Handle(
            Command(content: null, templateCode: "OTP", parameters: new() { ["code"] = "1" }),
            TestContext.Current.CancellationToken);

        _added!.Body.Should().Be("Özel 1");
    }

    [Fact]
    public async Task Handle_OtherOrganizationsTemplate_IsNotUsed()
    {
        _templates.Add(Template(organizationId: 99, "Başkasının {{code}}"));

        var act = () => _handler.Handle(
            Command(content: null, templateCode: "OTP", parameters: new() { ["code"] = "1" }),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Fact]
    public async Task Handle_InactiveTemplate_IsNotUsed()
    {
        _templates.Add(Template(null, "Pasif {{code}}", status: GeneralStatus.Passive));

        var act = () => _handler.Handle(
            Command(content: null, templateCode: "OTP", parameters: new() { ["code"] = "1" }),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Fact]
    public async Task Handle_TemplateForDifferentChannel_IsNotUsed()
    {
        _templates.Add(Template(null, "E-posta gövdesi", channel: Channel.Email, subject: "Konu"));

        var act = () => _handler.Handle(
            Command(channel: Channel.Sms, content: null, templateCode: "OTP"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [Fact]
    public async Task Handle_MissingTemplateParameter_ThrowsInvalidInput()
    {
        _templates.Add(Template(null, "Kodunuz: {{code}}"));

        var act = () => _handler.Handle(
            Command(content: null, templateCode: "OTP", parameters: new()),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailTemplate_RendersSubjectFromTemplate()
    {
        _templates.Add(Template(null, "Hoş geldin {{name}}", Channel.Email, subject: "Merhaba {{name}}"));

        await _handler.Handle(
            Command(Channel.Email, "ali@site.com", content: null, templateCode: "OTP",
                    parameters: new() { ["name"] = "Ali" }),
            TestContext.Current.CancellationToken);

        _added!.Subject.Should().Be("Merhaba Ali");
        _added.Body.Should().Be("Hoş geldin Ali");
    }

    [Fact]
    public async Task Handle_EmailWithoutAnySubject_ThrowsInvalidInput()
    {
        var act = () => _handler.Handle(
            Command(Channel.Email, "ali@site.com", subject: null, content: "Gövde"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    // ── Idempotency ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SameKeyAndSameMessage_ReturnsExistingWithoutCreatingAnother()
    {
        var existing = new OutboxMessage
        {
            Id = Guid.NewGuid(), OrganizationId = OrganizationId, Channel = Channel.Sms,
            Recipient = "+905551112233", IdempotencyKey = "key-1", Body = "eski"
        };
        _storedMessages.Add(existing);

        var response = await _handler.Handle(Command(key: "key-1"), TestContext.Current.CancellationToken);

        response.Id.Should().Be(existing.Id);
        _messagesMock.Verify(
            x => x.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameKeyDifferentRecipient_ThrowsConflict()
    {
        _storedMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), OrganizationId = OrganizationId, Channel = Channel.Sms,
            Recipient = "+905550000000", IdempotencyKey = "key-1"
        });

        var act = () => _handler.Handle(Command(key: "key-1"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SameKeyFromAnotherOrganization_IsNotADuplicate()
    {
        _storedMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), OrganizationId = 99, Channel = Channel.Sms,
            Recipient = "+905551112233", IdempotencyKey = "key-1"
        });

        await _handler.Handle(Command(key: "key-1"), TestContext.Current.CancellationToken);

        _added.Should().NotBeNull();
        _added!.OrganizationId.Should().Be(OrganizationId);
    }
}