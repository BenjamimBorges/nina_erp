using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class MobileService(AppDbContext db) : IMobileService
{
    public async Task<MobileSummaryDto> GetSummaryAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        var company = await db.Companies.AsNoTracking().Where(x => x.Id == companyId && x.IsActive).Select(x => new { x.TimeZoneId }).SingleAsync(ct);
        var (fromUtc, toUtc) = LocalDayUtc(company.TimeZoneId);
        var salesToday = await db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && x.Status == SaleStatus.Completed && x.SaleDate >= fromUtc && x.SaleDate < toUtc)
            .Select(x => new { Net = x.TotalProducts - x.TotalDiscount }).ToListAsync(ct);
        var receivedToday = await db.PaymentTransactions.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && x.CapturedAt >= fromUtc && x.CapturedAt < toUtc &&
            (x.Status == PaymentTransactionStatus.Captured || x.Status == PaymentTransactionStatus.PartiallyRefunded || x.Status == PaymentTransactionStatus.Refunded))
            .SumAsync(x => x.Amount - x.RefundedAmount, ct);
        var lowStock = await db.InventoryBalances.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .Join(db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive), b => b.ProductId, p => p.Id, (b, p) => new { Available = b.OnHand - b.Reserved, p.StockMin })
            .CountAsync(x => x.Available <= x.StockMin, ct);
        var pendingPayments = await db.PaymentTransactions.AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.IsActive &&
            (x.Status == PaymentTransactionStatus.Pending || x.Status == PaymentTransactionStatus.Processing || x.Status == PaymentTransactionStatus.Authorized || x.Status == PaymentTransactionStatus.ReconciliationRequired), ct);
        var pendingFiscal = await db.Invoices.AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.IsActive &&
            (x.Status == InvoiceStatus.Draft || x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.Processing || x.Status == InvoiceStatus.Contingency || x.Status == InvoiceStatus.Error || x.Status == InvoiceStatus.ProviderNotConfigured), ct);
        var openCash = await db.CashSessions.AsNoTracking().Where(x => x.CompanyId == companyId && x.UserId == userId && x.IsActive && x.Status == CashSessionStatus.Open)
            .Join(db.CashRegisters.AsNoTracking(), s => s.CashRegisterId, r => r.Id, (s, r) => new { r.Name }).FirstOrDefaultAsync(ct);
        return new(salesToday.Sum(x => x.Net), receivedToday, salesToday.Count, lowStock, pendingPayments, pendingFiscal, openCash is not null, openCash?.Name);
    }

    public async Task<IReadOnlyList<MobileRecentSaleDto>> GetRecentSalesAsync(Guid companyId, int limit, CancellationToken ct) =>
        await db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderByDescending(x => x.SaleDate).Take(Math.Clamp(limit, 1, 50))
            .Select(x => new MobileRecentSaleDto(x.Id, x.Client != null ? x.Client.Name : null, x.TotalProducts - x.TotalDiscount, x.Status.ToString(), x.SaleDate)).ToListAsync(ct);

    public async Task<IReadOnlyList<MobileLowStockDto>> GetLowStockAsync(Guid companyId, int limit, CancellationToken ct) =>
        await db.InventoryBalances.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .Join(db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive), b => b.ProductId, p => p.Id, (b, p) => new { b, p })
            .Join(db.Warehouses.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive), x => x.b.WarehouseId, w => w.Id, (x, w) => new { x.b, x.p, w })
            .Where(x => x.b.OnHand - x.b.Reserved <= x.p.StockMin)
            .OrderBy(x => x.b.OnHand - x.b.Reserved).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new MobileLowStockDto(x.p.Id, x.p.Sku, x.p.Name, x.b.OnHand - x.b.Reserved, x.p.StockMin, x.w.Name)).ToListAsync(ct);

    private static (DateTime FromUtc, DateTime ToUtc) LocalDayUtc(string? zoneId)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(zoneId) ? "America/Sao_Paulo" : zoneId); }
        catch { zone = TimeZoneInfo.Utc; }
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var startLocal = DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        return (TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), TimeZoneInfo.ConvertTimeToUtc(endLocal, zone));
    }
}
