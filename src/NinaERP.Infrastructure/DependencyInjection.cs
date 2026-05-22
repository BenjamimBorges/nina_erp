using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Auth;
using NinaERP.Infrastructure.Data;
using NinaERP.Infrastructure.Logging;
using NinaERP.Infrastructure.Repositories;
using NinaERP.Infrastructure.Repositories.Interfaces;

namespace NinaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<NinaErpDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly("NinaERP.Infrastructure")));

        // Repositories
        services.AddScoped<ISupplierRepository, SupplierRepository>();

        // Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddStructuredLogging();

        return services;
    }
}

