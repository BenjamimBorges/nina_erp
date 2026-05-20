using Microsoft.Extensions.DependencyInjection;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Auth;
using NinaERP.Infrastructure.Logging;

namespace NinaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddStructuredLogging();
        return services;
    }
}
