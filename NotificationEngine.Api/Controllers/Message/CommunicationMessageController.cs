using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NotificationEngine.Core.Enums;
using NotificationEngine.Api.Mapping;
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Api.Security;
using NotificationEngine.Api.Services;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Api.Controllers.TMFOpenApi5;

/// <summary>TMF681 Communication Management API v4 — communicationMessage kaynağı.</summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthConstants.SchemeName)]
[Route("communication-management/v4/communicationMessage")]
[Produces("application/json")]
public class CommunicationMessageController : ControllerBase
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ISendNowRateLimiter _rateLimiter;

    public CommunicationMessageController(
        IMediator mediator, ICurrentUserService currentUser, ISendNowRateLimiter rateLimiter)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _rateLimiter = rateLimiter;
    }

    /// <summary>Mesajı kuyruğa alır. Gönderim arka planda yapılır; Idempotency-Key header'ı zorunlu.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommunicationMessageModel>> Create(
        [FromBody] CreateCommunicationMessageModel model,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new InvalidInputException("'Idempotency-Key' header'ı zorunlu.");

        var response = await _mediator.Send(model.ToCommand(idempotencyKey), ct);
        var result = response.ToModel(HrefFor(response.Id));

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, result);
    }

    /// <summary>
    /// Anında gönderim (OTP). Outbox'a yazmaz, içeriği saklamaz, yeniden denemez.
    /// TMF681 v4 şemasında tanımlı bir operasyon değil; TMF-tarzı özel aksiyon ucu.
    /// </summary>
    [HttpPost("send")]
    [EnableRateLimiting("send-now")]   // kuruluş başına dakikalık genel sınır (Program.cs)
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CommunicationMessageModel>> Send(
        [FromBody] CreateCommunicationMessageModel model, CancellationToken ct)
    {
        var command = model.ToSendNowCommand();

        // Aynı numaraya art arda SMS (SMS bombardımanı / fatura şişirme) engellenir.
        var organizationId = await _currentUser.GetOrganizationIdAsync(ct);
        var decision = _rateLimiter.Check(organizationId, command.Recipient);
        if (!decision.Allowed)
        {
            Response.Headers.RetryAfter = ((int)Math.Ceiling(decision.RetryAfter.TotalSeconds)).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = "429",
                reason = "Too Many Requests",
                message = "Bu numaraya kısa sürede çok fazla gönderim yapıldı.",
                status = "429"
            });
        }

        var result = await _mediator.Send(command, ct);

        // İçerik ve alıcı cevapta ASLA yankılanmaz.
        var response = new CommunicationMessageModel
        {
            Type = "CommunicationMessage",
            MessageType = command.Channel == Channel.Email ? "Email" : "SMS",
            State = result.Success ? "completed" : "failed",
            SendTime = result.Success ? new DateTimeOffset(DateTime.SpecifyKind(result.SentAt, DateTimeKind.Utc)) : null
        };
        return result.Success ? Ok(response) : StatusCode(StatusCodes.Status502BadGateway, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunicationMessageModel>> GetById(Guid id, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetMessageQuery(id), ct);
        return Ok(response.ToModel(HrefFor(response.Id)));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CommunicationMessageModel>>> List(
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        var query = new GetMessageListQuery(
            Math.Max(offset ?? 0, 0),
            Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit),
            CommunicationMessageMappings.ToStatuses(state));

        var result = await _mediator.Send(query, ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result.Items.Select(i => i.ToModel(HrefFor(i.Id))).ToList());
    }

    private string HrefFor(Guid id) => Url.Action(nameof(GetById), new { id }) ?? string.Empty;
}