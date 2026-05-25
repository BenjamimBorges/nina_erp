using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;
public class StockMovementRepository : BaseRepository<StockMovement>, IStockMovementRepository
{
    public StockMovementRepository(AppDbContext db) : base(db) { }

    public async Task<IReadOnlyList<StockMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _set.Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.MovedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<StockMovement>> GetByCompanyAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _set.Include(x => x.Product)
            .Where(x => x.CompanyId == companyId && x.MovedAt >= from && x.MovedAt <= to)
            .OrderByDescending(x => x.MovedAt).ToListAsync(ct);
}
