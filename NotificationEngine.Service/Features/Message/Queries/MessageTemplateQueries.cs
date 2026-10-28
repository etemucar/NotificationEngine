using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Queries;

/// <summary>Kuruluş kendi şablonunu ve ortak şablonları okuyabilir; başkasınınki "bulunamadı" döner.</summary>
public sealed record GetMessageTemplateQuery(int Id, int? OrganizationId) : IRequest<MessageTemplateResponse>;

/// <summary>OrganizationId boşsa (SiteAdmin) yalnızca ortak şablonlar listelenir.</summary>
public sealed record GetMessageTemplateListQuery(
    int? OrganizationId,
    bool IncludeShared,
    int Offset,
    int Limit,
    string? Code,
    Channel? Channel,
    GeneralStatus? Status) : IRequest<MessageTemplateListResponse>;