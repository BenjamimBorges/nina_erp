using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(Guid companyId, int take, CancellationToken ct = default) =>
        await _set.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);
}
