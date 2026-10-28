using MediatR;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;
/// <summary>
/// Worker'ın her turda gönderdiği komut. ITransactionalRequest DEĞİL: sağlayıcı çağrısı
/// açık bir transaction içinde beklememeli (claim kendi kısa transaction'ını açar).
/// </summary>
public sealed record ProcessOutboxCommand : IRequest<ProcessOutboxResponse>;