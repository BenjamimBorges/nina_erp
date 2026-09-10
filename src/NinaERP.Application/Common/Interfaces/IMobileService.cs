namespace NinaERP.Application.Common.Interfaces;

public sealed record MobileSummaryDto(decimal SalesToday, decimal ReceivedToday, int SalesCountToday,
    int LowStockCount, int PendingPayments, int PendingFiscal, bool HasOpenCashSession, string? CashRegisterName);
public sealed record MobileRecentSaleDto(Guid Id, string? ClientName, decimal Total, string Status, DateTime SaleDate);
public sealed record MobileLowStockDto(Guid ProductId, string Sku, string Name, decimal Available, decimal Minimum, string Warehouse);

public interface IMobileService
{
    Task<MobileSummaryDto> GetSummaryAsync(Guid companyId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<MobileRecentSaleDto>> GetRecentSalesAsync(Guid companyId, int limit, CancellationToken ct);
    Task<IReadOnlyList<MobileLowStockDto>> GetLowStockAsync(Guid companyId, int limit, CancellationToken ct);
}
