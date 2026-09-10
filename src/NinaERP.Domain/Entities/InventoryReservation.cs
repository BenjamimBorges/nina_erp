using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class InventoryReservation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid SaleId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }
    public Sale? Sale { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Product? Product { get; set; }

    public decimal ActiveQuantity => Quantity - ReleasedQuantity - ConsumedQuantity;
}
