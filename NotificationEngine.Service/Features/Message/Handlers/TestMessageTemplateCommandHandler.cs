using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class TestMessageTemplateCommandHandler
    : IRequestHandler<TestMessageTemplateCommand, SendNowResponse>
{
    private const string TestSubjectPrefix = "[TEST] ";

    private static readonly IReadOnlyDictionary<string, string> NoParameters =
        new Dictionary<string, string>();

    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IMediator _mediator;

    public TestMessageTemplateCommandHandler(IRepository<MessageTemplate, int> templates, IMediator mediator)
    {
        _templates = templates;
        _mediator = mediator;
    }

    public async Task<SendNowResponse> Handle(TestMessageTemplateCommand request, CancellationToken ct)
    {
        var template = await _templates.GetByIdAsync(request.Id, ct);

        // Kuruluşa özel şablon yalnızca sahibi tarafından denenir; ortak şablon herkes tarafından.
        if (template is null
            || (template.OrganizationId is not null && template.OrganizationId != request.OrganizationId))
            throw new NotFoundException($"Şablon bulunamadı: {request.Id}");

        var parameters = request.Parameters ?? NoParameters;

        // Eksik parametre burada 400 olur; hiçbir şey gönderilmez.
        var body = TemplateRenderer.Render(template.Body, parameters);
        var subject = template.Subject is null ? null : TemplateRenderer.Render(template.Subject, parameters);

        // Gerçek bir mesajla karışmasın diye yalnızca e-posta konusuna işaret eklenir (SMS bölünmesin).
        if (template.Channel == Channel.Email && subject is not null)
            subject = TestSubjectPrefix + subject;

        // Gönderim anında gönderim hattından geçer: alıcı/kanal doğrulaması, içeriğin saklanmaması, hata davranışı.
        return await _mediator.Send(
            new SendNowCommand(template.Channel, request.Recipient.Trim(), subject, body, null, null, null), ct);
    }
}