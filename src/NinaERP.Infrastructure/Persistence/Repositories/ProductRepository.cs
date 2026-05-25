using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<Product?> GetBySkuAsync(Guid companyId, string sku, CancellationToken ct = default) =>
        await _set.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Sku == sku && x.IsActive, ct);

    public async Task<IReadOnlyList<Product>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(Guid companyId, string term, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive &&
            (x.Name.Contains(term) || x.Sku.Contains(term) || x.Barcode.Contains(term) || x.Department.Contains(term)))
            .OrderBy(x => x.Name).Take(100).ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive && x.StockQty <= x.StockMin)
            .OrderBy(x => x.StockQty).ToListAsync(ct);

    public async Task<int> CountAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.CountAsync(x => x.CompanyId == companyId && x.IsActive, ct);

    public async Task<decimal> GetInventoryValueAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive)
            .SumAsync(x => x.StockQty * x.CostAverage, ct);
}
