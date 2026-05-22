using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public decimal TotalProducts { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalPaid { get; set; }
    public string Status { get; set; } = "Pending";
    public string FiscalModel { get; set; } = "NFe"; // NFe, NFCe, etc
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public Invoice? Invoice { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
