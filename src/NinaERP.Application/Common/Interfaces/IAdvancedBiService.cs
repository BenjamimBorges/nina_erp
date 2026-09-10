namespace NinaERP.Application.Common.Interfaces;

public sealed record BiMonthlyDto(DateTime Month, int SalesCount, decimal NetSales, decimal Cost, decimal Margin,
    decimal Received, decimal PayablesOpen, decimal ReceivablesOpen);
public sealed record BiSellerDto(Guid UserId, string SellerName, DateTime Month, int SalesCount, decimal NetSales, decimal AverageTicket);
public sealed record BiPaymentMixDto(DateTime Month, string Method, int Transactions, decimal GrossCaptured, decimal Refunded, decimal NetReceived);
public sealed record BiExecutiveSummaryDto(decimal NetSales12Months, decimal Margin12Months, decimal Received12Months,
    decimal PayablesOpen, decimal ReceivablesOpen, decimal MarginPercent, IReadOnlyList<BiMonthlyDto> Monthly);

public interface IAdvancedBiService
{
    Task<IReadOnlyList<BiMonthlyDto>> GetMonthlyAsync(Guid companyId, int months, CancellationToken ct);
    Task<IReadOnlyList<BiSellerDto>> GetSellersAsync(Guid companyId, int months, int limit, CancellationToken ct);
    Task<IReadOnlyList<BiPaymentMixDto>> GetPaymentMixAsync(Guid companyId, int months, CancellationToken ct);
    Task<BiExecutiveSummaryDto> GetExecutiveSummaryAsync(Guid companyId, int months, CancellationToken ct);
    Task RefreshAsync(CancellationToken ct);
}
