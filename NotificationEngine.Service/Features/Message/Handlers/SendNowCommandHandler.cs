using MediatR;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Interfaces;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class SendNowCommandHandler : IRequestHandler<SendNowCommand, SendNowResponse>
{
    private static readonly IReadOnlyDictionary<string, string> NoParameters =
        new Dictionary<string, string>();

    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly IChannelProviderResolver _resolver;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<SendNowCommandHandler> _logger;

    public SendNowCommandHandler(
        IRepository<MessageTemplate, int> templates,
        IChannelProviderResolver resolver,
        ICurrentUserService currentUser,
        TimeProvider time,
        ILogger<SendNowCommandHandler> logger)
    {
        _templates = templates;
        _resolver = resolver;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<SendNowResponse> Handle(SendNowCommand request, CancellationToken ct)
    {
        var organizationId = await _currentUser.GetOrganizationIdAsync(ct);

        var subject = request.Subject;
        var body = request.Content;

        if (string.IsNullOrWhiteSpace(body))
        {
            var templateCode = request.TemplateCode!;
            var culture = request.Culture ?? NotificationConstants.DefaultCulture;

            // Kuruluşa özel şablon, ortak (OrganizationId = null) şablonun önüne geçer.
            var candidates = await _templates.FindAsync(
                t => t.Code == templateCode
                     && t.Channel == request.Channel
                     && t.Culture == culture
                     && t.Status == GeneralStatus.Active
                     && (t.OrganizationId == organizationId || t.OrganizationId == null),
                orderBy: q => q.OrderByDescending(t => t.OrganizationId != null),
                ct: ct);

            var template = candidates.FirstOrDefault()
                ?? throw new InvalidInputException(
                    $"'{templateCode}' şablonu ({request.Channel}, {culture}) bulunamadı.");

            var parameters = request.Parameters ?? NoParameters;
            body = TemplateRenderer.Render(template.Body, parameters);
            subject ??= template.Subject is null ? null : TemplateRenderer.Render(template.Subject, parameters);
        }

        // CreateMessage ile aynı kural: konusuz e-posta sağlayıcıya gitmeden reddedilir.
        if (request.Channel == Channel.Email && string.IsNullOrWhiteSpace(subject))
            throw new InvalidInputException("E-posta için konu belirlenemedi.");

        // Gövde yalnızca bu RenderedMessage içinde, bellekte yaşar; veritabanına ve log'a yazılmaz.
        var rendered = new RenderedMessage(
            Guid.NewGuid(), request.Channel, request.Recipient, subject, body,
            null, null, MessagePriority.High);

        SendResult result;
        try
        {
            result = await _resolver.Resolve(request.Channel).SendAsync(rendered, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // İstisna metni alıcıyı içerebilir: yalnızca tipi loglanır.
            _logger.LogError("Anında gönderim sağlayıcı hatası: {ExceptionType}", ex.GetType().Name);
            result = SendResult.Transient("Sağlayıcı hatası.");
        }

        _logger.LogInformation("Anında gönderim {Channel} → {Recipient}: {Success} ({FailureKind})",
            request.Channel, RecipientMasker.Mask(request.Recipient), result.Success, result.FailureKind);

        if (result.FailureKind == SendFailureKind.InvalidRecipient)
            throw new InvalidInputException("Alıcı geçersiz.");

        return new SendNowResponse(result.Success, result.FailureKind, _time.GetUtcNow().UtcDateTime);
    }
}