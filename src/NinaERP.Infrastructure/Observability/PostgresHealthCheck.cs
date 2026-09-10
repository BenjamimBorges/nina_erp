using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NinaERP.Infrastructure.Persistence;
namespace NinaERP.Infrastructure.Observability;
public sealed class PostgresHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy("PostgreSQL acessível") : HealthCheckResult.Unhealthy("PostgreSQL indisponível"); }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("Falha ao verificar PostgreSQL", ex); }
    }
}
