using System.Linq.Expressions;
using AutoMapper;
using MediatR;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class GetMessageTemplateListQueryHandler
    : IRequestHandler<GetMessageTemplateListQuery, MessageTemplateListResponse>
{
    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IMapper _mapper;

    public GetMessageTemplateListQueryHandler(IRepository<MessageTemplate, int> templates, IMapper mapper)
    {
        _templates = templates;
        _mapper = mapper;
    }

    public async Task<MessageTemplateListResponse> Handle(GetMessageTemplateListQuery request, CancellationToken ct)
    {
        var organizationId = request.OrganizationId;
        var includeShared = request.IncludeShared;
        var code = request.Code;
        var channel = request.Channel;
        var status = request.Status;

        Expression<Func<MessageTemplate, bool>> predicate = t =>
            (t.OrganizationId == organizationId || (includeShared && t.OrganizationId == null))
            && (code == null || t.Code == code)
            && (channel == null || t.Channel == channel)
            && (status == null || t.Status == status);

        var items = await _templates.FindOffsetAsync(
            predicate,
            request.Offset,
            request.Limit,
            orderBy: q => q.OrderBy(t => t.Code).ThenBy(t => t.Channel).ThenBy(t => t.Culture),
            ct: ct);

        var total = await _templates.CountAsync(predicate, ct);

        return new MessageTemplateListResponse(
            items.Select(t => _mapper.Map<MessageTemplateResponse>(t)).ToList(),
            total);
    }
}