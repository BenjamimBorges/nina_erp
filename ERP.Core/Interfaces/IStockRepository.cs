using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface IStockRepository : IRepository<Stock>
    {
        Task<Stock?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}
