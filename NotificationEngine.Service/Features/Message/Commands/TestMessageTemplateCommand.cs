using MediatR;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>
/// Şablonu örnek parametrelerle işleyip alıcıya GERÇEKTEN gönderir (anında gönderim yolu: içerik saklanmaz).
/// Pasif şablon da denenebilir. ITransactionalRequest DEĞİL: veritabanına yazmaz.
/// </summary>
public sealed record TestMessageTemplateCommand(
    int Id,
    int? OrganizationId,
    string Recipient,
    IReadOnlyDictionary<string, string>? Parameters) : IRequest<SendNowResponse>;