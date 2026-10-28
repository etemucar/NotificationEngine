using System.Linq.Expressions;
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

public sealed class CreateMessageTemplateCommandHandler
    : IRequestHandler<CreateMessageTemplateCommand, MessageTemplateResponse>
{
    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateMessageTemplateCommandHandler(
        IRepository<MessageTemplate, int> templates, IUnitOfWork uow, IMapper mapper)
    {
        _templates = templates;
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<MessageTemplateResponse> Handle(CreateMessageTemplateCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim();

        // Pasif şablon da anahtarı tutar (benzersiz index hepsini kapsar): yeni şablon yerine aktif etmek gerekir.
        Expression<Func<MessageTemplate, bool>> sameKey = t =>
            t.OrganizationId == request.OrganizationId
            && t.Code == code
            && t.Channel == request.Channel
            && t.Culture == request.Culture;

        const string conflictMessage =
            "Bu kod, kanal ve kültür için şablon zaten var (pasifse yeniden aktif edin).";

        if (await _templates.AnyAsync(sameKey, ct))
            throw new ConflictException(conflictMessage);

        var entity = new MessageTemplate
        {
            OrganizationId = request.OrganizationId,
            Code = code,
            Channel = request.Channel,
            Culture = request.Culture,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject,
            Body = request.Body,
            Status = GeneralStatus.Active   // ModelBase varsayılanı Created: şablon araması Active ister
        };

        try
        {
            await _templates.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Eşzamanlı aynı anahtar: benzersiz index ikinci kaydı reddeder. Kazanan varsa 409, yoksa hata gerçektir.
            if (await _templates.AnyAsync(sameKey, ct))
                throw new ConflictException(conflictMessage);
            throw;
        }

        return _mapper.Map<MessageTemplateResponse>(entity);
    }
}