using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => (Qty * UnitPrice) - Discount;
}
