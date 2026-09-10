using NinaERP.Application.Common.Interfaces;

namespace NinaERP.API.HostedServices;

public sealed class NodeHeartbeatWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NodeHeartbeatWorker> logger) : BackgroundService
{
    private readonly string _nodeId = configuration["Distributed:NodeId"] ?? $"{Environment.MachineName}:{Environment.ProcessId}";
    private readonly string _role = configuration["Distributed:Role"] ?? "api";
    private readonly string _version = configuration["Release:Version"] ?? typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("Distributed:HeartbeatSeconds", 15), 5, 300));
    private readonly bool _enabled = configuration.GetValue("Features:DistributedScale", false);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            logger.LogInformation("Coordenação distribuída desabilitada por feature flag.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cluster = scope.ServiceProvider.GetRequiredService<IClusterCoordinationService>();
                await cluster.HeartbeatAsync(_nodeId, _role, _version, null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Falha ao registrar heartbeat do nó {NodeId}.", _nodeId); }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
