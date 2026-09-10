using NinaERP.Application.Common.Interfaces;
using NinaERP.Infrastructure.Persistence.Repositories;

namespace NinaERP.Infrastructure.Persistence;
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    private IProductRepository? _products;
    private IClientRepository? _clients;
    private ISupplierRepository? _suppliers;
    private IDepartmentRepository? _departments;
    private IStockMovementRepository? _stockMovements;
    private IUserRepository? _users;
    private IAuditLogRepository? _auditLogs;
    private ISaleRepository? _sales;

    public IProductRepository Products => _products ??= new ProductRepository(_db);
    public IClientRepository Clients => _clients ??= new ClientRepository(_db);
    public ISupplierRepository Suppliers => _suppliers ??= new SupplierRepository(_db);
    public IDepartmentRepository Departments => _departments ??= new DepartmentRepository(_db);
    public IStockMovementRepository StockMovements => _stockMovements ??= new StockMovementRepository(_db);
    public IUserRepository Users => _users ??= new UserRepository(_db);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_db);
    public ISaleRepository Sales => _sales ??= new SaleRepository(_db);

    public async Task<int> CommitAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
