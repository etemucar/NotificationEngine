using MediatR;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;
public sealed class ProcessOutboxCommandHandler : IRequestHandler<ProcessOutboxCommand, ProcessOutboxResponse>
{
    private const int MaxErrorLength = 2000;

    private readonly IOutboxStore _store;
    private readonly IChannelProviderResolver _resolver;
    private readonly IRepository<DeliveryLog, long> _logs;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    private readonly ILogger<ProcessOutboxCommandHandler> _logger;

    public ProcessOutboxCommandHandler(
        IOutboxStore store,
        IChannelProviderResolver resolver,
        IRepository<DeliveryLog, long> logs,
        IUnitOfWork uow,
        TimeProvider time,
        ILogger<ProcessOutboxCommandHandler> logger)
    {
        _store = store;
        _resolver = resolver;
        _logs = logs;
        _uow = uow;
        _time = time;
        _logger = logger;
    }

    public async Task<ProcessOutboxResponse> Handle(ProcessOutboxCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var claimed = await _store.ClaimDueAsync(
            NotificationConstants.ClaimBatchSize,
            TimeSpan.FromSeconds(NotificationConstants.LockDurationSeconds),
            now, ct);

        if (claimed.Count == 0)
            return new ProcessOutboxResponse(0, 0, 0, 0);

        int sent = 0, retried = 0, failed = 0;

        foreach (var message in claimed)
        {
            SendResult result;
            try
            {
                var rendered = new RenderedMessage(
                    message.Id, message.Channel, message.Recipient,
                    message.Subject, message.Body ?? string.Empty,
                    null, null, message.Priority); // gönderen: sağlayıcının yapılandırmadaki varsayılanı

                result = await _resolver.Resolve(message.Channel).SendAsync(rendered, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // kalan mesajlar Processing kalır, kilit dolunca yeniden alınır
            }
            catch (Exception ex)
            {
                // İstisna metni alıcıyı içerebilir: log'a ve LastError'a yalnızca tipi yazılır.
                _logger.LogError("Sağlayıcı hatası: {MessageId} → {Recipient} ({ExceptionType})",
                    message.Id, RecipientMasker.Mask(message.Recipient), ex.GetType().Name);
                result = SendResult.Transient($"Sağlayıcı hatası ({ex.GetType().Name}).");
            }

            switch (await ApplyAsync(message, result, now, ct))
            {
                case MessageStatus.Sent: sent++; break;
                case MessageStatus.Pending: retried++; break;
                default: failed++; break;
            }
        }

        return new ProcessOutboxResponse(claimed.Count, sent, retried, failed);
    }

    private async Task<MessageStatus> ApplyAsync(
        OutboxMessage message, SendResult result, DateTime now, CancellationToken ct)
    {
        message.LockedUntil = null;
        DeliveryEventType evt;
        string? detail = null;

        if (result.Success)
        {
            message.DeliveryStatus = MessageStatus.Sent;
            message.SentAt = now;
            message.ProviderMessageId = result.ProviderMessageId;
            message.LastError = null;
            evt = DeliveryEventType.Accepted;
        }
        else
        {
            detail = Truncate(result.Error);
            message.LastError = detail;

            var canRetry = result.FailureKind == SendFailureKind.Transient
                           && message.AttemptCount < NotificationConstants.MaxAttempts;

            if (canRetry)
            {
                var delay = TimeSpan.FromSeconds(
                    NotificationConstants.BaseRetryDelaySeconds * Math.Pow(2, message.AttemptCount - 1));
                message.DeliveryStatus = MessageStatus.Pending;
                message.NextAttemptAt = now.Add(delay);
                evt = DeliveryEventType.Attempted;
            }
            else
            {
                message.DeliveryStatus = MessageStatus.Failed;
                evt = result.FailureKind == SendFailureKind.InvalidRecipient
                    ? DeliveryEventType.InvalidRecipient
                    : DeliveryEventType.Failed;
            }
        }

        await _logs.AddAsync(new DeliveryLog
        {
            MessageId = message.Id,
            Event = evt,
            Detail = detail,
            OccurredAt = now
        }, ct);

        // Mesaj başına kaydet: bir mesajın sonucu, sonrakilerin hatasıyla kaybolmasın.
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Outbox {MessageId} {Channel} → {Status} (deneme {Attempt})",
            message.Id, message.Channel, message.DeliveryStatus, message.AttemptCount);

        return message.DeliveryStatus;
    }

    private static string? Truncate(string? text) =>
        text is { Length: > MaxErrorLength } ? text[..MaxErrorLength] : text;
}