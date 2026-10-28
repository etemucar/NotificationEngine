using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Api.Controllers.TMFOpenApi5;

[Authorize]
[Route("digital-identity/v5/digitalIdentity")]
[ApiController]
[Produces("application/json")]
public class DigitalIdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public DigitalIdentityController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper   = mapper;
    }

    /// <summary>Yeni DigitalIdentity oluşturur (sadece SITE_ADMIN)</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] DigitalIdentityModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command  = _mapper.Map<CreateDigitalIdentityCommand>(model);
        var result   = await _mediator.Send(command);
        var response = _mapper.Map<DigitalIdentityModel>(result);

        return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
    }

    /// <summary>DigitalIdentity'yi kısmi günceller (nickname, credentials) — kendi kaydı ya da SITE_ADMIN</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] DigitalIdentityModel model)
    {
        var command = _mapper.Map<PatchDigitalIdentityCommand>(model);
        command.DigitalIdentityId = id;

        var result   = await _mediator.Send(command);
        var response = _mapper.Map<DigitalIdentityModel>(result);

        return Ok(response);
    }

    /// <summary>DigitalIdentity statüsünü değiştirir (sadece SITE_ADMIN)</summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDigitalIdentityStatusModel model)
    {
        var result = await _mediator.Send(new UpdateDigitalIdentityStatusCommand(
            DigitalIdentityId: id,
            Status: model.Status
        ));

        return Ok(new { success = result });
    }
}
