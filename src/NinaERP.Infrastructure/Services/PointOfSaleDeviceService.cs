using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
namespace NinaERP.Infrastructure.Services;
public sealed class PointOfSaleDeviceService : IPointOfSaleDeviceService
{
    public Task<PointOfSaleDeviceStatus> GetStatusAsync(CancellationToken ct) => Task.FromResult(new PointOfSaleDeviceStatus(false, false, "Camada de periféricos pronta; driver/bridge local ainda não configurado."));
    public Task PrintReceiptAsync(Guid saleId, CancellationToken ct) => throw new BusinessException("Impressora térmica não configurada. Configure um adapter local antes de imprimir.");
    public Task OpenCashDrawerAsync(CancellationToken ct) => throw new BusinessException("Gaveta de dinheiro não configurada. Configure um adapter local antes de abrir.");
}
