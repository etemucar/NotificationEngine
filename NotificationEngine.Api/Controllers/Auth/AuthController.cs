using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationEngine.Api.Models;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Responses;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Queries;

namespace NotificationEngine.Api.Controllers;

[Authorize]
[Route("api/v1/auth")]
[ApiController]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly bool _returnTokenInBody;

    public AuthController(IMediator mediator, ICurrentUserService currentUserService, IConfiguration config, IMapper mapper)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _returnTokenInBody = config.GetValue<bool>("Auth:ReturnTokenInBody");
    }

    /// <summary>Login</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _mediator.Send(new LoginCommand
        {
            Identifier = model.Identifier,
            Password   = model.Password
        });

        Response.Cookies.Append("auth", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.AccessTokenExpiration,
            Path     = "/"
        });

        Response.Cookies.Append("refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.RefreshTokenExpiration,
            Path     = "/api/v1/auth/refresh"
        });

        return Ok(new AuthResponse
        {
            Success        = true,
            UserId         = result.UserId,
            UserName       = result.UserName,
            UserIdentifier = result.UserIdentifier,
        });
    }

    /// <summary>Refresh access token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refresh"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new ErrorResponse("Refresh token eksik."));

        var result = await _mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = refreshToken
        });

        Response.Cookies.Append("auth", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.AccessTokenExpiration,
            Path     = "/"
        });

        Response.Cookies.Append("refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.RefreshTokenExpiration,
            Path     = "/api/v1/auth/refresh"
        });

        return Ok(new { success = true });
    }

    /// <summary>Logout</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refresh"];
        if (!string.IsNullOrEmpty(refreshToken))
            await _mediator.Send(new RevokeTokenCommand { RefreshToken = refreshToken });

        Response.Cookies.Delete("auth");
        Response.Cookies.Delete("refresh");

        return Ok(new { success = true });
    }

    /// <summary>Register</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _mediator.Send(new RegisterCommand
        {
            GivenName  = model.GivenName,
            FamilyName = model.FamilyName,
            Identifier = model.Identifier,
            Password   = model.Password,
            LanguageId = model.LanguageId
        });

        Response.Cookies.Append("auth", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.AccessTokenExpiration,
            Path     = "/"
        });

        Response.Cookies.Append("refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = result.RefreshTokenExpiration,
            Path     = "/api/v1/auth/refresh"
        });

        if (_returnTokenInBody)
            return Ok(new { success = true, token = result.AccessToken });

        return Ok(new { success = true });
    }    

    /// <summary>Mevcut kullanıcı bilgisi</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Kullanıcı doğrulanamadı.");

        var result = await _mediator.Send(new GetCurrentUserQuery(userId));
        return Ok(result);
    }    

    /// <summary>
    /// Organization kaydeder (Party + Organization + PartyRole + DigitalIdentity
    /// + Credentials). PartyRoleTypeId = ExternalService ise ApplicationUser
    /// oluşturulmaz (NotificationEngine/DocDes gibi sadece ApiKey ile çalışan servisler);
    /// başka rol tipinde ApplicationUser da oluşturulur (insan login edebilir).
    /// Sadece SiteAdmin çağırabilir.
    /// </summary>
    [HttpPost("register-organization")]
    [Authorize(Policy = "SiteAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterOrganization([FromBody] RegisterOrganizationModel model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command = _mapper.Map<RegisterOrganizationCommand>(model);
        var result = await _mediator.Send(command);

        return Ok(new RegisterOrganizationResponse
        {
            Success           = true,
            PartyId           = result.PartyId,
            OrganizationId    = result.OrganizationId,
            PartyRoleId       = result.PartyRoleId,
            DigitalIdentityId = result.DigitalIdentityId,
            ApplicationUserId = result.ApplicationUserId,
            IssuedCredentials = result.IssuedCredentials.Select(c => new IssuedCredentialResponseItem
            {
                CredentialId      = c.CredentialId,
                CredentialType    = c.CredentialType.ToString(),
                GeneratedRawValue = c.GeneratedRawValue
            }).ToList()
        });
    }
}