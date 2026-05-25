using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;
public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<IReadOnlyList<StockMovement>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetByCompanyAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct = default);
}
