using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    }
}
