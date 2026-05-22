using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Purchase : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal TotalProducts { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalPaid { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public Invoice? Invoice { get; set; }
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
