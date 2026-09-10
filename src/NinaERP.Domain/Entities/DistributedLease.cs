using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class DistributedLease : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public long Version { get; set; }
}

public sealed class SystemNode : BaseEntity
{
    public string NodeId { get; set; } = string.Empty;
    public string Role { get; set; } = "api";
    public string Version { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
}
