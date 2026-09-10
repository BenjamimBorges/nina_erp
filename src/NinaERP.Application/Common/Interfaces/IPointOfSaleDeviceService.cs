namespace NinaERP.Application.Common.Interfaces;

public sealed record PointOfSaleDeviceStatus(bool ThermalPrinterConfigured, bool CashDrawerConfigured, string Message);

public interface IPointOfSaleDeviceService
{
    Task<PointOfSaleDeviceStatus> GetStatusAsync(CancellationToken ct);
    Task PrintReceiptAsync(Guid saleId, CancellationToken ct);
    Task OpenCashDrawerAsync(CancellationToken ct);
}
