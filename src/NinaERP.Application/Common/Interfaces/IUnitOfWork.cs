namespace NinaERP.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IClientRepository Clients { get; }
    ISupplierRepository Suppliers { get; }
    IDepartmentRepository Departments { get; }
    IStockMovementRepository StockMovements { get; }
    IUserRepository Users { get; }
    IAuditLogRepository AuditLogs { get; }
    ISaleRepository Sales { get; }
    Task<int> CommitAsync(CancellationToken ct = default);
}
