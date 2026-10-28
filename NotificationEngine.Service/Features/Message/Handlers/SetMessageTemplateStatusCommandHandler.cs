using AutoMapper;
using MediatR;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class SetMessageTemplateStatusCommandHandler
    : IRequestHandler<SetMessageTemplateStatusCommand, MessageTemplateResponse>
{
    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public SetMessageTemplateStatusCommandHandler(
        IRepository<MessageTemplate, int> templates, IUnitOfWork uow, IMapper mapper)
    {
        _templates = templates;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<MessageTemplateResponse> Handle(SetMessageTemplateStatusCommand request, CancellationToken ct)
    {
        var template = await _templates.GetByIdAsync(request.Id, ct);
        if (template is null || template.OrganizationId != request.OrganizationId)
            throw new NotFoundException($"Şablon bulunamadı: {request.Id}");

        template.Status = request.Active ? GeneralStatus.Active : GeneralStatus.Passive;

        await _templates.UpdateAsync(template, ct);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<MessageTemplateResponse>(template);
    }
}