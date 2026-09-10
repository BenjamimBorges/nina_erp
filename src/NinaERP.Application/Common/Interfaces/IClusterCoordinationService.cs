namespace NinaERP.Application.Common.Interfaces;

public sealed record ClusterNodeDto(string NodeId, string Role, string Version, DateTime StartedAt, DateTime LastHeartbeatAt, bool Healthy);

public interface IClusterCoordinationService
{
    Task<bool> TryAcquireLeaseAsync(string name, string ownerId, TimeSpan ttl, CancellationToken ct);
    Task ReleaseLeaseAsync(string name, string ownerId, CancellationToken ct);
    Task HeartbeatAsync(string nodeId, string role, string version, string? metadataJson, CancellationToken ct);
    Task<IReadOnlyList<ClusterNodeDto>> GetNodesAsync(TimeSpan healthyWindow, CancellationToken ct);
}
