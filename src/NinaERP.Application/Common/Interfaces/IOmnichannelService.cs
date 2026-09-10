using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record ChannelOrderItemInput(string ExternalSku, string? Barcode, decimal Quantity, decimal UnitPrice, decimal Discount = 0);
public sealed record ReceiveChannelOrderInput(SalesChannel Channel, string ExternalOrderId, string? ExternalCustomerId,
    string? ClientDocument, string Currency, decimal Total, string IdempotencyKey, IReadOnlyList<ChannelOrderItemInput> Items);
public sealed record ChannelOrderItemDto(Guid Id, string ExternalSku, string? Barcode, decimal Quantity, decimal UnitPrice,
    decimal Discount, Guid? ProductId);
public sealed record ChannelOrderDto(Guid Id, SalesChannel Channel, string ExternalOrderId, ChannelOrderStatus Status,
    string? ClientDocument, string Currency, decimal Total, string? ValidationError, Guid? ImportedSaleId,
    DateTime ReceivedAt, DateTime? ValidatedAt, DateTime? ImportedAt, IReadOnlyList<ChannelOrderItemDto> Items);
public sealed record ChannelCatalogItemDto(Guid ProductId, string Sku, string Barcode, string Name, decimal Price,
    decimal Available, DateTime UpdatedAt);

public interface IOmnichannelService
{
    Task<ChannelOrderDto> ReceiveAsync(Guid companyId, Guid userId, ReceiveChannelOrderInput input, CancellationToken ct);
    Task<IReadOnlyList<ChannelOrderDto>> GetOrdersAsync(Guid companyId, ChannelOrderStatus? status, int limit, CancellationToken ct);
    Task<IReadOnlyList<ChannelCatalogItemDto>> GetCatalogAsync(Guid companyId, int limit, CancellationToken ct);
    Task<ChannelOrderDto> MarkImportedAsync(Guid companyId, Guid orderId, Guid saleId, CancellationToken ct);
}
