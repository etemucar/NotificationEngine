using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>
/// Anında, eşzamanlı gönderim (OTP gibi kullanıcının beklediği mesajlar). Outbox'a YAZMAZ:
/// mesaj içeriği hiçbir yerde saklanmaz, yeniden deneme yoktur. Veritabanına yazmadığı için
/// ITransactionalRequest değildir.
/// </summary>
public sealed record SendNowCommand(
    Channel Channel,
    string Recipient,
    string? Subject,
    string? Content,
    string? TemplateCode,
    IReadOnlyDictionary<string, string>? Parameters,
    string? Culture) : IRequest<SendNowResponse>;