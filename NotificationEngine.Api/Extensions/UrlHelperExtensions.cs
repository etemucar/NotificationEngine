using Microsoft.AspNetCore.Mvc;

namespace NotificationEngine.Api.Extensions;

public static class UrlHelperExtensions
{
    public static string BuildHref(
        this IUrlHelper urlHelper,
        object id,
        string actionName,
        string? controllerName = null)
    {
        return urlHelper.ActionLink(
            action: actionName,
            controller: controllerName,
            values: new { id },
            protocol: urlHelper.ActionContext.HttpContext.Request.Scheme,
            host: urlHelper.ActionContext.HttpContext.Request.Host.Value)
            ?? throw new InvalidOperationException("Href oluşturulamadı.");
    }
}
