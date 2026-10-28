using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Requests;
using AutoMapper;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Api.Extensions;

namespace NotificationEngine.Api.Controllers.TMFOpenApi5;

/// <summary>
/// TMF632 - Party Management API v5 / Individual
/// </summary>
[Authorize(Policy = "SiteAdmin")]
[Route("party-management/v5/individual")]
[ApiController]
[Produces("application/json")]
public class IndividualController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public IndividualController(IMapper mapper, IMediator mediator)
    {
        _mapper = mapper;
        _mediator = mediator;
    }

    /// <summary>Creates an Individual</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IndividualModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateIndividualModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command = _mapper.Map<CreateIndividualCommand>(model);
        var result = await _mediator.Send(command);
        var response = _mapper.Map<IndividualModel>(result);
        response.Href = Url.BuildHref(result.Id, nameof(GetById));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
    }

    /// <summary>Retrieves an Individual by id</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IndividualModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var result = await _mediator.Send(new GetIndividualQuery { Id = id });

        if (result is null)
            return NotFound();

        var response  = _mapper.Map<IndividualModel>(result);
        response.Href = Url.BuildHref(result.Id, nameof(GetById));

        return Ok(response);
    }

    /// <summary>List or find Individual objects</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IndividualModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int?    offset,
        [FromQuery] int?    limit,
        [FromQuery] string? fields)
    {
        var result = await _mediator.Send(new GetIndividualListQuery
        {
            Offset = offset ?? 0,
            Limit  = limit  ?? 20,
            Fields = fields
        });

        return Ok(result);
    }

    /// <summary>
    /// Updates partially an Individual (JSON Merge Patch).
    /// contactMedium gönderilmezse dokunulmaz; gönderilirse (boş liste dahil) kişinin kendi iletişim
    /// kayıtları silinip gelenler yazılır. relatedParty PATCH ile değiştirilemez.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(IndividualModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch([FromRoute] int id, [FromBody] UpdateIndividualModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (model.RelatedParty is { Count: > 0 })
            throw new InvalidInputException("İlişkili taraflar (relatedParty) PATCH ile değiştirilemez.");

        var command = new PatchIndividualCommand
        {
            Id             = id,
            GivenName      = model.GivenName,
            FamilyName     = model.FamilyName,
            MiddleName     = model.MiddleName,
            Title          = model.Title,
            Gender         = model.Gender,
            Nationality    = model.Nationality,
            BirthDate      = model.BirthDate,
            PlaceOfBirth   = model.PlaceOfBirth,
            CountryOfBirth = model.CountryOfBirth,
            MaritalStatus  = model.MaritalStatus,
            ValidForStart  = model.ValidFor?.StartDateTime,
            ValidForEnd    = model.ValidFor?.EndDateTime,
            ContactMedium  = model.ContactMedium is null
                ? null
                : _mapper.Map<List<ContactMediumRequest>>(model.ContactMedium)
        };

        var result = await _mediator.Send(command);

        if (result is null)
            return NotFound();
        var response  = _mapper.Map<IndividualModel>(result);
        response.Href = Url.BuildHref(result.Id, nameof(GetById));

        return Ok(response);
    }

    /// <summary>Deletes an Individual</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var deleted = await _mediator.Send(new DeleteIndividualCommand { Id = id });

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}