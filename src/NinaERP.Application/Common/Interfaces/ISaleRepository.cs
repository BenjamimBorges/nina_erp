using NinaERP.Domain.Entities;

namespace NinaERP.Application.Common.Interfaces;

public interface ISaleRepository : IRepository<Sale>
{
    Task<Sale?> GetByIdempotencyKeyAsync(Guid companyId, string idempotencyKey, CancellationToken ct = default);
    Task<Sale?> GetDetailsAsync(Guid companyId, Guid saleId, CancellationToken ct = default);
    Task<IReadOnlyList<Sale>> GetRecentAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct = default);
}
