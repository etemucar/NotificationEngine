using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>OrganizationId boşsa ortak şablon. Controller kimlik doğrulamadan belirler; handler kullanıcı bilmez.</summary>
public sealed record CreateMessageTemplateCommand(
    int? OrganizationId,
    string Code,
    Channel Channel,
    string Culture,
    string? Subject,
    string Body) : IRequest<MessageTemplateResponse>;

/// <summary>Kod, kanal ve kültür (benzersiz anahtar) değişmez; yalnızca konu ve gövde güncellenir.</summary>
public sealed record UpdateMessageTemplateCommand(
    int Id,
    int? OrganizationId,
    string? Subject,
    string Body) : IRequest<MessageTemplateResponse>;

public sealed record SetMessageTemplateStatusCommand(
    int Id,
    int? OrganizationId,
    bool Active) : IRequest<MessageTemplateResponse>;