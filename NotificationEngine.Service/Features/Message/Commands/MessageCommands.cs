using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>
/// Mesajı outbox'a yazar (gönderim worker'da). İki biçim: Content dolu → olduğu gibi kullanılır;
/// Content boş + TemplateCode dolu → NotificationEngine şablonundan üretilir.
/// Tek satıra yazdığı için ITransactionalRequest DEĞİL.
/// </summary>
public sealed record CreateMessageCommand(
    Channel Channel,
    string Recipient,
    string? Subject,
    string? Content,
    string? TemplateCode,
    IReadOnlyDictionary<string, string>? Parameters,
    MessagePriority Priority,
    DateTimeOffset? ScheduledSendTime,
    string? Culture,
    string IdempotencyKey) : IRequest<MessageResponse>;