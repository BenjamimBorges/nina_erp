using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class ChannelOrder : BaseEntity
{
    public Guid CompanyId { get; set; }
    public SalesChannel Channel { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public ChannelOrderStatus Status { get; set; } = ChannelOrderStatus.Received;
    public string? ExternalCustomerId { get; set; }
    public string? ClientDocument { get; set; }
    public string Currency { get; set; } = "BRL";
    public decimal Total { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ValidationError { get; set; }
    public Guid? ImportedSaleId { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ImportedAt { get; set; }
    public long Version { get; set; }
    public ICollection<ChannelOrderItem> Items { get; set; } = new List<ChannelOrderItem>();
}

public sealed class ChannelOrderItem : BaseEntity
{
    public Guid ChannelOrderId { get; set; }
    public string ExternalSku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public Guid? ProductId { get; set; }
    public ChannelOrder? ChannelOrder { get; set; }
    public Product? Product { get; set; }
}
