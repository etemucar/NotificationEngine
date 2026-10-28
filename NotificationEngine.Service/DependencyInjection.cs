using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Service.Common;

namespace NotificationEngine.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPartyRoleIdResolver, PartyRoleIdResolver>();
        services.AddScoped<ILoginAttemptLimiter, LoginAttemptLimiter>();
        return services;
    }
}