using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class PurchaseOrder : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid WarehouseId { get; set; }
    public string Number { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public long Version { get; set; }
    public decimal TotalProducts { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalFreight { get; set; }
    public decimal TotalTaxes { get; set; }
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public Company? Company { get; set; }
    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<AccountPayable> Payables { get; set; } = new List<AccountPayable>();
}
