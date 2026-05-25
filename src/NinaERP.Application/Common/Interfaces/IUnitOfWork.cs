namespace NinaERP.Application.Common.Interfaces;
public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IClientRepository Clients { get; }
    ISupplierRepository Suppliers { get; }
    IStockMovementRepository StockMovements { get; }
    IUserRepository Users { get; }
    Task<int> CommitAsync(CancellationToken ct = default);
}
