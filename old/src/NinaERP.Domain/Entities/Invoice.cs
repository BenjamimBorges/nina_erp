using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Model { get; set; } = "NFe"; // NFe, NFCe, etc
    public int Series { get; set; }
    public int Number { get; set; }
    public string AccessKey { get; set; } = string.Empty; // UK
    public string XmlPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Authorized, Canceled
    public DateTime? AuthorizedAt { get; set; }
    public string CancelReason { get; set; } = string.Empty;

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public ICollection<InvoiceEvent> Events { get; set; } = new List<InvoiceEvent>();
}
