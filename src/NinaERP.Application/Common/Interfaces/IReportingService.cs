namespace NinaERP.Application.Common.Interfaces;

public sealed record DailySalesReport(DateTime Date, decimal GrossSales, decimal Discounts, decimal NetSales, decimal Received, decimal Cancelled, int SalesCount);
public sealed record ProductSalesReport(Guid ProductId, string Sku, string ProductName, decimal Quantity, decimal GrossSales, decimal NetSales, decimal Cost, decimal Margin);
public sealed record CashReport(Guid CashSessionId, string RegisterName, DateTime OpenedAt, DateTime? ClosedAt, decimal OpeningAmount, decimal Receipts, decimal Withdrawals, decimal Refunds, decimal ExpectedAmount, decimal? CountedAmount, decimal? Difference);
public sealed record FinancialSummaryReport(decimal PayablesOpen, decimal ReceivablesOpen, decimal PayablesPaid, decimal ReceivablesReceived, decimal OverduePayables, decimal OverdueReceivables);

public interface IReportingService
{
    Task<IReadOnlyList<DailySalesReport>> GetDailySalesAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct);
    Task<IReadOnlyList<ProductSalesReport>> GetProductSalesAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct);
    Task<IReadOnlyList<CashReport>> GetCashAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct);
    Task<FinancialSummaryReport> GetFinancialSummaryAsync(Guid companyId, CancellationToken ct);
}
