using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total => OrderedQty * UnitCost - Discount + Taxes;
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }
}
