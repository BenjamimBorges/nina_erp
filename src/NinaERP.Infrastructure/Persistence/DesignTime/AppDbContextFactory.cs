using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NinaERP.Infrastructure.Persistence.DesignTime;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NINAERP_EF_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=nina_erp_design;Username=ninaerp;Pooling=false;Timeout=5";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
