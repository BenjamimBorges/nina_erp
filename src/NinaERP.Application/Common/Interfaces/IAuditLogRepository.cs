using NinaERP.Domain.Entities;

namespace NinaERP.Application.Common.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(Guid companyId, int take, CancellationToken ct = default);
}
