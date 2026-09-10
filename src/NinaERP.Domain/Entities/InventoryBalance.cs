using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class InventoryBalance : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
    public decimal AverageCost { get; set; }
    public long Version { get; set; }
    public decimal Available => OnHand - Reserved;
    public Company? Company { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Product? Product { get; set; }
}
