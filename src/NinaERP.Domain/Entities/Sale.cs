using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
