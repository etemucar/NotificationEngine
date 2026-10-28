using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Handlers;

namespace NotificationEngine.Service.Tests.Features.Handlers;

public class ProcessOutboxCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IChannelProviderResolver> _resolverMock = new();
    private readonly Mock<IChannelProvider> _providerMock = new();
    private readonly Mock<IRepository<DeliveryLog, long>> _logsMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly List<DeliveryLog> _addedLogs = new();
    private readonly ProcessOutboxCommandHandler _handler;

    public ProcessOutboxCommandHandlerTests()
    {
        _resolverMock.Setup(x => x.Resolve(It.IsAny<Channel>())).Returns(_providerMock.Object);

        _logsMock
            .Setup(x => x.AddAsync(It.IsAny<DeliveryLog>(), It.IsAny<CancellationToken>()))
            .Callback<DeliveryLog, CancellationToken>((log, _) => _addedLogs.Add(log))
            .Returns(Task.CompletedTask);

        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ProcessOutboxCommandHandler(
            _storeMock.Object,
            _resolverMock.Object,
            _logsMock.Object,
            _uowMock.Object,
            new FixedTimeProvider(Now),
            new Mock<ILogger<ProcessOutboxCommandHandler>>().Object);
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────

    private void Claims(params OutboxMessage[] messages) =>
        _storeMock
            .Setup(x => x.ClaimDueAsync(
                It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<OutboxMessage>)messages);

    private void ProviderReturns(SendResult result) =>
        _providerMock
            .Setup(x => x.SendAsync(It.IsAny<RenderedMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    // Claim sonrası halini taklit eder: Processing, kilitli, AttemptCount claim'de artırılmış.
    private static OutboxMessage NewMessage(int attempt = 1) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = 1,
        Channel = Channel.Sms,
        Recipient = "+905551112233",
        Body = "Kodunuz: 1234",
        IdempotencyKey = Guid.NewGuid().ToString(),
        DeliveryStatus = MessageStatus.Processing,
        AttemptCount = attempt,
        LockedUntil = Now.AddMinutes(2)
    };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    // ── Senaryolar ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoDueMessages_ReturnsZeroAndDoesNotCallProvider()
    {
        Claims();

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(0, 0, 0, 0));
        _providerMock.Verify(
            x => x.SendAsync(It.IsAny<RenderedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_MarksSentAndLogsAccepted()
    {
        var message = NewMessage();
        Claims(message);
        ProviderReturns(SendResult.Ok("job-42"));

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(1, 1, 0, 0));
        message.DeliveryStatus.Should().Be(MessageStatus.Sent);
        message.SentAt.Should().Be(Now);
        message.ProviderMessageId.Should().Be("job-42");
        message.LockedUntil.Should().BeNull();
        message.LastError.Should().BeNull();
        _addedLogs.Should().ContainSingle(l =>
            l.MessageId == message.Id && l.Event == DeliveryEventType.Accepted && l.OccurredAt == Now);
    }

    [Theory]
    [InlineData(1, 1)]   // 1. denemeden sonra: taban gecikme × 1
    [InlineData(2, 2)]   // × 2
    [InlineData(3, 4)]   // × 4 (üstel artış)
    public async Task Handle_TransientFailureWithAttemptsLeft_SchedulesRetryWithExponentialBackoff(
        int attemptCount, int multiplier)
    {
        attemptCount.Should().BeLessThan(NotificationConstants.MaxAttempts);
        var message = NewMessage(attemptCount);
        Claims(message);
        ProviderReturns(SendResult.Transient("zaman aşımı"));

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(1, 0, 1, 0));
        message.DeliveryStatus.Should().Be(MessageStatus.Pending);
        message.NextAttemptAt.Should().Be(
            Now.AddSeconds(NotificationConstants.BaseRetryDelaySeconds * multiplier));
        message.LockedUntil.Should().BeNull();
        message.LastError.Should().Be("zaman aşımı");
        _addedLogs.Should().ContainSingle(l => l.Event == DeliveryEventType.Attempted);
    }

    [Fact]
    public async Task Handle_TransientFailureAtMaxAttempts_MarksFailed()
    {
        var message = NewMessage(NotificationConstants.MaxAttempts);
        Claims(message);
        ProviderReturns(SendResult.Transient("hâlâ hata"));

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(1, 0, 0, 1));
        message.DeliveryStatus.Should().Be(MessageStatus.Failed);
        _addedLogs.Should().ContainSingle(l => l.Event == DeliveryEventType.Failed);
    }

    [Fact]
    public async Task Handle_PermanentFailure_MarksFailedWithoutRetry()
    {
        var message = NewMessage(attempt: 1);
        Claims(message);
        ProviderReturns(SendResult.Permanent("kimlik hatası"));

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(1, 0, 0, 1));
        message.DeliveryStatus.Should().Be(MessageStatus.Failed);
        message.LastError.Should().Be("kimlik hatası");
        _addedLogs.Should().ContainSingle(l => l.Event == DeliveryEventType.Failed);
    }

    [Fact]
    public async Task Handle_InvalidRecipient_MarksFailedAndLogsInvalidRecipient()
    {
        var message = NewMessage();
        Claims(message);
        ProviderReturns(SendResult.InvalidRecipient("numara geçersiz"));

        await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        message.DeliveryStatus.Should().Be(MessageStatus.Failed);
        _addedLogs.Should().ContainSingle(l => l.Event == DeliveryEventType.InvalidRecipient);
    }

    [Fact]
    public async Task Handle_ProviderThrows_IsTreatedAsTransient()
    {
        var message = NewMessage(attempt: 1);
        Claims(message);
        _providerMock
            .Setup(x => x.SendAsync(It.IsAny<RenderedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(1, 0, 1, 0));
        message.DeliveryStatus.Should().Be(MessageStatus.Pending);
        // İstisna metni alıcıyı içerebilir: LastError'a yalnızca istisna tipi yazılır.
        message.LastError.Should().Be("Sağlayıcı hatası (InvalidOperationException).");
        message.LastError.Should().NotContain("boom");
    }

    [Fact]
    public async Task Handle_LongErrorText_IsTruncatedTo2000Characters()
    {
        var message = NewMessage();
        Claims(message);
        ProviderReturns(SendResult.Permanent(new string('x', 3_000)));

        await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        message.LastError!.Length.Should().Be(2_000);
    }

    [Fact]
    public async Task Handle_MultipleMessages_SavesAfterEachOne()
    {
        // Bir mesajın sonucu, sonrakilerin hatasıyla kaybolmasın diye mesaj başına kayıt.
        Claims(NewMessage(), NewMessage(), NewMessage());
        ProviderReturns(SendResult.Ok());

        var result = await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        result.Should().Be(new ProcessOutboxResponse(3, 3, 0, 0));
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_FromAddressIsNotSetByHandler_ProviderUsesItsOwnDefault()
    {
        Claims(NewMessage());
        RenderedMessage? captured = null;
        _providerMock
            .Setup(x => x.SendAsync(It.IsAny<RenderedMessage>(), It.IsAny<CancellationToken>()))
            .Callback<RenderedMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(SendResult.Ok());

        await _handler.Handle(new ProcessOutboxCommand(), TestContext.Current.CancellationToken);

        captured.Should().NotBeNull();
        captured!.FromEmail.Should().BeNull();
        captured.FromName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CancellationRequested_RethrowsAndSavesNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Claims(NewMessage());
        _providerMock
            .Setup(x => x.SendAsync(It.IsAny<RenderedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var act = () => _handler.Handle(new ProcessOutboxCommand(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}