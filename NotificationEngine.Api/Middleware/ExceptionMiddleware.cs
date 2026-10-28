using System.Text.Json;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Core.Responses;

namespace NotificationEngine.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorResponse) = ex switch
        {
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, new ErrorResponse(e.Message)),
            ForbiddenException     e => (StatusCodes.Status403Forbidden,    new ErrorResponse(e.Message)),
            NotFoundException     e => (StatusCodes.Status404NotFound,     new ErrorResponse(e.Message)),
            ConflictException     e => (StatusCodes.Status409Conflict,     new ErrorResponse(e.Message)),
            InvalidInputException e => (StatusCodes.Status400BadRequest,   new ErrorResponse(e.Message)),
            FluentValidation.ValidationException e => (StatusCodes.Status400BadRequest, BuildValidationError(e)),
            BusinessException e => (StatusCodes.Status400BadRequest, new ErrorResponse(e.Message)),
            _ => (StatusCodes.Status500InternalServerError, new ErrorResponse("Beklenmeyen bir hata oluştu."))
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "İşlenmeyen hata: {Path}", context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
    }

    private static ErrorResponse BuildValidationError(FluentValidation.ValidationException e)
    {
        var fieldErrors = e.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

        return new ErrorResponse("Doğrulama hatası.", fieldErrors);
    }
}
