using AutoMapper;
using MediatR;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class GetMessageTemplateQueryHandler
    : IRequestHandler<GetMessageTemplateQuery, MessageTemplateResponse>
{
    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IMapper _mapper;

    public GetMessageTemplateQueryHandler(IRepository<MessageTemplate, int> templates, IMapper mapper)
    {
        _templates = templates;
        _mapper = mapper;
    }

    public async Task<MessageTemplateResponse> Handle(GetMessageTemplateQuery request, CancellationToken ct)
    {
        var template = await _templates.GetByIdAsync(request.Id, ct);

        // Ortak şablon (OrganizationId boş) herkese açık okunur; kuruluşa özel olan yalnızca sahibine.
        if (template is null
            || (template.OrganizationId is not null && template.OrganizationId != request.OrganizationId))
            throw new NotFoundException($"Şablon bulunamadı: {request.Id}");

        return _mapper.Map<MessageTemplateResponse>(template);
    }
}