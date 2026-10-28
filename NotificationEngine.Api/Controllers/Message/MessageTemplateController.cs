using System.Security.Claims;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NotificationEngine.Api.Mapping;
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Api.Security;
using NotificationEngine.Api.Services;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Constants;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Api.Controllers.TMFOpenApi5;

/// <summary>
/// TMF681 Communication Management API v4 ailesinde TMF-tarzı kaynak: messageTemplate (TMF'de tanımlı değil).
/// ApiKey ile gelen kuruluş yalnızca kendi şablonlarını, SiteAdmin (JWT) yalnızca ortak şablonları yönetir.
/// Başkasının şablonu "bulunamadı" döner.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthConstants.SchemeName + "," + JwtBearerDefaults.AuthenticationScheme)]
[Route("communication-management/v4/messageTemplate")]
[Produces("application/json")]
public class MessageTemplateController : ControllerBase
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ISendNowRateLimiter _rateLimiter;
    private readonly IMapper _mapper;

    public MessageTemplateController(
        IMediator mediator, ICurrentUserService currentUser, ISendNowRateLimiter rateLimiter, IMapper mapper)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _rateLimiter = rateLimiter;
        _mapper = mapper;
    }

    /// <summary>Yeni şablon ekler. Kod + kanal + kültür kuruluş içinde benzersizdir.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageTemplateModel>> Create(
        [FromBody] CreateMessageTemplateModel model, CancellationToken ct)
    {
        var organizationId = await ResolveOrganizationAsync(ct);

        var response = await _mediator.Send(new CreateMessageTemplateCommand(
            organizationId,
            model.Code,
            CommunicationMessageMappings.ParseChannel(model.MessageType),
            model.Culture ?? NotificationConstants.DefaultCulture,
            model.Subject,
            model.Body), ct);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ToModel(response));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageTemplateModel>> GetById(int id, CancellationToken ct)
    {
        var organizationId = await ResolveOrganizationAsync(ct);
        var response = await _mediator.Send(new GetMessageTemplateQuery(id, organizationId), ct);
        return Ok(ToModel(response));
    }

    /// <summary>Kuruluş kendi şablonlarını (ve includeShared=true ise ortak olanları), SiteAdmin ortak şablonları listeler.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MessageTemplateModel>>> List(
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        [FromQuery] string? code,
        [FromQuery] string? messageType,
        [FromQuery] string? status,
        [FromQuery] bool? includeShared,
        CancellationToken ct)
    {
        var organizationId = await ResolveOrganizationAsync(ct);

        var result = await _mediator.Send(new GetMessageTemplateListQuery(
            organizationId,
            includeShared ?? true,
            Math.Max(offset ?? 0, 0),
            Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit),
            string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            string.IsNullOrWhiteSpace(messageType) ? null : CommunicationMessageMappings.ParseChannel(messageType),
            ParseStatus(status)), ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result.Items.Select(ToModel).ToList());
    }

    /// <summary>
    /// Merge-patch: yalnızca gönderilen alanlar (subject, body, status) değişir. Kod, kanal ve kültür değişmez.
    /// status "passive" şablonu pasife alır (silinmez), "active" yeniden aktif eder.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageTemplateModel>> Patch(
        int id, [FromBody] UpdateMessageTemplateModel model, CancellationToken ct)
    {
        var organizationId = await ResolveOrganizationAsync(ct);
        var active = ParseActive(model.Status);

        if (model.Subject is null && model.Body is null && active is null)
            throw new InvalidInputException("Güncellenecek en az bir alan verilmeli (subject, body, status).");

        // Başka kuruluşun şablonu burada "bulunamadı" döner. Gönderilmeyen alanlar mevcut değerlerden tamamlanır.
        var current = await _mediator.Send(new GetMessageTemplateQuery(id, organizationId), ct);
        var response = current;

        if (model.Subject is not null || model.Body is not null)
        {
            response = await _mediator.Send(new UpdateMessageTemplateCommand(
                id, organizationId, model.Subject ?? current.Subject, model.Body ?? current.Body), ct);
        }

        if (active is not null)
            response = await _mediator.Send(new SetMessageTemplateStatusCommand(id, organizationId, active.Value), ct);

        return Ok(ToModel(response));
    }

    /// <summary>
    /// Şablonu verilen parametrelerle işleyip alıcıya GERÇEKTEN gönderir (içerik saklanmaz).
    /// Pasif şablon da denenebilir. E-posta konusunun başına "[TEST]" eklenir.
    /// </summary>
    [HttpPost("{id:int}/test")]
    [EnableRateLimiting("send-now")]   // kuruluş başına dakikalık genel sınır (Program.cs)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MessageTemplateTestResultModel>> Test(
        int id, [FromBody] TestMessageTemplateModel model, CancellationToken ct)
    {
        var organizationId = await ResolveOrganizationAsync(ct);

        // Aynı alıcıya art arda mesaj atılmasını sınırlar (SiteAdmin için sayaç kuruluşu 0).
        var decision = _rateLimiter.Check(organizationId ?? 0, model.Recipient);
        if (!decision.Allowed)
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(decision.RetryAfter.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = "429",
                reason = "Too Many Requests",
                message = "Bu alıcıya kısa sürede çok fazla gönderim yapıldı.",
                status = "429"
            });
        }

        var result = await _mediator.Send(
            new TestMessageTemplateCommand(id, organizationId, model.Recipient, model.Parameters), ct);

        var response = new MessageTemplateTestResultModel
        {
            State = result.Success ? "completed" : "failed",
            SendTime = result.Success
                ? new DateTimeOffset(DateTime.SpecifyKind(result.SentAt, DateTimeKind.Utc))
                : null
        };

        return result.Success ? Ok(response) : StatusCode(StatusCodes.Status502BadGateway, response);
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────

    /// <summary>
    /// SiteAdmin → null (ortak şablonlar). ApiKey ile gelen kuruluş → kendi OrganizationId'si.
    /// Başka her kimlik (örn. sıradan JWT kullanıcısı) → 403.
    /// </summary>
    private async Task<int?> ResolveOrganizationAsync(CancellationToken ct)
    {
        if (User.HasClaim(ClaimTypes.Role, ReferenceData.PartyRoleType.SiteAdminCd))
            return null;

        if (User.Identity?.AuthenticationType == ApiKeyAuthConstants.SchemeName)
            return await _currentUser.GetOrganizationIdAsync(ct);

        throw new ForbiddenException("Bu işlem için yetkiniz yok.");
    }

    private static GeneralStatus? ParseStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "active" => GeneralStatus.Active,
        "passive" => GeneralStatus.Passive,
        _ => throw new InvalidInputException($"Geçersiz status: '{status}' (active, passive).")
    };

    private static bool? ParseActive(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "active" => true,
        "passive" => false,
        _ => throw new InvalidInputException($"Geçersiz status: '{status}' (active, passive).")
    };

    private MessageTemplateModel ToModel(MessageTemplateResponse response)
    {
        var model = _mapper.Map<MessageTemplateModel>(response);
        model.Href = HrefFor(response.Id);
        return model;
    }

    private string HrefFor(int id) => Url.Action(nameof(GetById), new { id }) ?? string.Empty;
}