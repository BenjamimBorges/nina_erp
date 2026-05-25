using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(Guid companyId, string sku, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchAsync(Guid companyId, string term, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(Guid companyId, CancellationToken ct = default);
    Task<int> CountAsync(Guid companyId, CancellationToken ct = default);
    Task<decimal> GetInventoryValueAsync(Guid companyId, CancellationToken ct = default);
}
