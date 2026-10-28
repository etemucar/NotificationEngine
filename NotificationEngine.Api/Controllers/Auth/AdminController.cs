using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationEngine.Api.Models;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Api.Controllers;

[Authorize(Policy = "SiteAdmin")] 
[Route("api/v1/admin")]
[ApiController]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Yeni site-admin kullanıcı oluştur (sadece mevcut site-admin çağırabilir)</summary>
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdminUser([FromBody] AdminUserModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _mediator.Send(new CreateAdminUserCommand
        {
            GivenName  = model.GivenName,
            FamilyName = model.FamilyName,
            Identifier = model.Identifier,
            Password   = model.Password,
            LanguageId = model.LanguageId
        });

        return Ok(new { success = true, userId = result.UserId });
    }
}
