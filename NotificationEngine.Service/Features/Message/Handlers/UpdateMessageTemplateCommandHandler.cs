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

public sealed class UpdateMessageTemplateCommandHandler
    : IRequestHandler<UpdateMessageTemplateCommand, MessageTemplateResponse>
{
    private const int MaxNonEmailBodyLength = 1_000;

    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateMessageTemplateCommandHandler(
        IRepository<MessageTemplate, int> templates, IUnitOfWork uow, IMapper mapper)
    {
        _templates = templates;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<MessageTemplateResponse> Handle(UpdateMessageTemplateCommand request, CancellationToken ct)
    {
        // Başka kuruluşun (ya da ortak) şablonu "bulunamadı" döner: varlığı sızdırılmaz.
        var template = await _templates.GetByIdAsync(request.Id, ct);
        if (template is null || template.OrganizationId != request.OrganizationId)
            throw new NotFoundException($"Şablon bulunamadı: {request.Id}");

        // Kanala bağlı kurallar (doğrulayıcı kanalı bilmez): oluşturmadakilerle aynı.
        if (template.Channel == Channel.Email && string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidInputException("E-posta şablonu için konu gerekli.");

        if (template.Channel != Channel.Email && request.Body.Length > MaxNonEmailBodyLength)
            throw new InvalidInputException(
                $"SMS ve push şablonlarının gövdesi en fazla {MaxNonEmailBodyLength} karakter olabilir.");

        template.Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject;
        template.Body = request.Body;

        await _templates.UpdateAsync(template, ct);
        await _uow.SaveChangesAsync(ct);

        return _mapper.Map<MessageTemplateResponse>(template);
    }
}