using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;

public sealed class SaleRepository : BaseRepository<Sale>, ISaleRepository
{
    public SaleRepository(AppDbContext db) : base(db) { }

    public Task<Sale?> GetByIdempotencyKeyAsync(Guid companyId, string idempotencyKey, CancellationToken ct = default) =>
        _set.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey && x.IsActive, ct);

    public Task<Sale?> GetDetailsAsync(Guid companyId, Guid saleId, CancellationToken ct = default) =>
        _set.Include(x => x.Client)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Payments)
            .Include(x => x.PaymentTransactions)
            .Include(x => x.Invoices).ThenInclude(x => x.Events)
            .Include(x => x.Warehouse)
            .Include(x => x.CashSession)
            .FirstOrDefaultAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct);

    public async Task<IReadOnlyList<Sale>> GetRecentAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _set.AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Payments)
            .Include(x => x.PaymentTransactions)
            .Where(x => x.CompanyId == companyId && x.SaleDate >= from && x.SaleDate <= to && x.IsActive)
            .OrderByDescending(x => x.SaleDate)
            .Take(500)
            .ToListAsync(ct);
}
