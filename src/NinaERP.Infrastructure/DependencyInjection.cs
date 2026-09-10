using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Auth;
using NinaERP.Infrastructure.Observability;
using NinaERP.Infrastructure.Persistence;
using NinaERP.Infrastructure.Services;

namespace NinaERP.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");
        services.AddDbContextPool<AppDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            npgsql.CommandTimeout(config.GetValue("Database:CommandTimeoutSeconds", 30));
        }), poolSize: config.GetValue("Database:DbContextPoolSize", 64));
        services.Configure<JwtSettings>(config.GetSection("Jwt"));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<ICashManagementService, CashManagementService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ISaleLifecycleService, SaleLifecycleService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<ITaxEngineService, TaxEngineService>();
        services.AddScoped<IFiscalService, FiscalService>();
        services.AddSingleton<IFiscalProvider, NotConfiguredFiscalProvider>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddSingleton<IPointOfSaleDeviceService, PointOfSaleDeviceService>();
        services.AddScoped<PostgresHealthCheck>();
        return services;
    }
}
