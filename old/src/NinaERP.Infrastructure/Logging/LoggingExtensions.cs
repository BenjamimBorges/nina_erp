using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace NinaERP.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/nina-erp-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .MinimumLevel.Information()
            .CreateLogger();

        services.AddLogging(lb => lb.AddSerilog());
        return services;
    }
}
