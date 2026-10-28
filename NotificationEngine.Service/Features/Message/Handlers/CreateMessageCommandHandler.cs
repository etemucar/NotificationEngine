using AutoMapper;
using MediatR;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public sealed class CreateMessageCommandHandler : IRequestHandler<CreateMessageCommand, MessageResponse>
{
    private static readonly IReadOnlyDictionary<string, string> NoParameters =
        new Dictionary<string, string>();

    private readonly IRepository<OutboxMessage, Guid> _messages;
    private readonly IRepository<MessageTemplate, int> _templates;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;

    public CreateMessageCommandHandler(
        IRepository<OutboxMessage, Guid> messages,
        IRepository<MessageTemplate, int> templates,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IMapper mapper,
        TimeProvider time)
    {
        _messages = messages;
        _templates = templates;
        _currentUser = currentUser;
        _uow = uow;
        _mapper = mapper;
        _time = time;
    }

    public async Task<MessageResponse> Handle(CreateMessageCommand request, CancellationToken ct)
    {
        var organizationId = await _currentUser.GetOrganizationIdAsync(ct);

        // Idempotency: aynı anahtar → mevcut mesajı dön; farklı içerikle gelirse çakışma.
        var existing = await FindExistingAsync(organizationId, request, ct);
        if (existing is not null)
            return existing;

        var subject = request.Subject;
        var body = request.Content;
        string? templateCode = null;

        if (string.IsNullOrWhiteSpace(body))
        {
            templateCode = request.TemplateCode!;

            // Outbox gövdeyi veritabanına düz yazar: kod/bağlantı taşıyan şablonlar kuyruğa alınmaz.
            if (IsSendNowOnly(templateCode))
                throw new InvalidInputException(
                    $"'{templateCode}' şablonu yalnızca anında gönderim (/send) ile kullanılabilir.");

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

        if (request.Channel == Channel.Email && string.IsNullOrWhiteSpace(subject))
            throw new InvalidInputException("E-posta için konu belirlenemedi.");

        var now = _time.GetUtcNow().UtcDateTime;

        var entity = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            TemplateCode = templateCode,
            Channel = request.Channel,
            Priority = request.Priority,
            Recipient = request.Recipient,
            Subject = subject,
            Body = body,
            IdempotencyKey = request.IdempotencyKey,
            DeliveryStatus = MessageStatus.Pending,
            NextAttemptAt = request.ScheduledSendTime?.UtcDateTime ?? now
        };

        try
        {
            await _messages.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Eşzamanlı aynı Idempotency-Key: unique index ikinci kaydı reddeder.
            // Kazanan kayıt varsa onu dön (gerekirse 409); yoksa hata gerçektir, olduğu gibi fırlat.
            var winner = await FindExistingAsync(organizationId, request, ct);
            if (winner is not null)
                return winner;
            throw;
        }

        return _mapper.Map<MessageResponse>(entity);
    }

    // Anında gönderim (/send) dışında kullanılmaması gereken şablon kodları.
    private static bool IsSendNowOnly(string code) =>
        code.Equals("PASSWORD_RESET", StringComparison.OrdinalIgnoreCase)
        || code.StartsWith("OTP_", StringComparison.OrdinalIgnoreCase);

    private async Task<MessageResponse?> FindExistingAsync(
        int organizationId, CreateMessageCommand request, CancellationToken ct)
    {
        var existing = await _messages.FindOneAsync(
            m => m.OrganizationId == organizationId && m.IdempotencyKey == request.IdempotencyKey, ct: ct);
        if (existing is null)
            return null;

        if (existing.Channel != request.Channel || existing.Recipient != request.Recipient)
            throw new ConflictException(
                "Bu idempotency anahtarı farklı bir mesaj için daha önce kullanılmış.");

        return _mapper.Map<MessageResponse>(existing);
    }
}