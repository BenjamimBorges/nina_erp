using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class ClusterCoordinationService(AppDbContext db) : IClusterCoordinationService
{
    public async Task<bool> TryAcquireLeaseAsync(string name, string ownerId, TimeSpan ttl, CancellationToken ct)
    {
        name = name.Trim(); ownerId = ownerId.Trim();
        if (name.Length is 0 or > 120 || ownerId.Length is 0 or > 160 || ttl <= TimeSpan.Zero) return false;
        var now = DateTime.UtcNow;
        var lease = await db.DistributedLeases.FirstOrDefaultAsync(x => x.Name == name && x.IsActive, ct);
        if (lease is null)
        {
            lease = new DistributedLease { Name = name, OwnerId = ownerId, AcquiredAt = now, ExpiresAt = now.Add(ttl) };
            db.DistributedLeases.Add(lease);
            try { await db.SaveChangesAsync(ct); return true; }
            catch (DbUpdateException) { db.Entry(lease).State = EntityState.Detached; return false; }
        }
        if (lease.OwnerId != ownerId && lease.ExpiresAt > now) return false;
        lease.OwnerId = ownerId; lease.AcquiredAt = now; lease.ExpiresAt = now.Add(ttl); lease.UpdatedAt = now;
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { return false; }
    }

    public async Task ReleaseLeaseAsync(string name, string ownerId, CancellationToken ct)
    {
        var lease = await db.DistributedLeases.FirstOrDefaultAsync(x => x.Name == name && x.OwnerId == ownerId && x.IsActive, ct);
        if (lease is null) return;
        lease.ExpiresAt = DateTime.UtcNow; lease.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { }
    }

    public async Task HeartbeatAsync(string nodeId, string role, string version, string? metadataJson, CancellationToken ct)
    {
        nodeId = nodeId.Trim();
        var now = DateTime.UtcNow;
        var node = await db.SystemNodes.FirstOrDefaultAsync(x => x.NodeId == nodeId && x.IsActive, ct);
        if (node is null)
        {
            node = new SystemNode { NodeId = nodeId, Role = role.Trim(), Version = version.Trim(), StartedAt = now, LastHeartbeatAt = now, MetadataJson = metadataJson };
            db.SystemNodes.Add(node);
        }
        else
        {
            node.Role = role.Trim(); node.Version = version.Trim(); node.LastHeartbeatAt = now; node.MetadataJson = metadataJson; node.UpdatedAt = now;
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) when (db.Entry(node).State == EntityState.Added)
        {
            db.Entry(node).State = EntityState.Detached;
            await db.SystemNodes.Where(x => x.NodeId == nodeId && x.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastHeartbeatAt, now).SetProperty(x => x.Version, version.Trim()).SetProperty(x => x.Role, role.Trim()).SetProperty(x => x.MetadataJson, metadataJson).SetProperty(x => x.UpdatedAt, now), ct);
        }
    }

    public async Task<IReadOnlyList<ClusterNodeDto>> GetNodesAsync(TimeSpan healthyWindow, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Subtract(healthyWindow);
        return await db.SystemNodes.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.LastHeartbeatAt).Take(200)
            .Select(x => new ClusterNodeDto(x.NodeId, x.Role, x.Version, x.StartedAt, x.LastHeartbeatAt, x.LastHeartbeatAt >= cutoff)).ToListAsync(ct);
    }
}
