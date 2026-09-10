using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;
public class Invoice : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? SaleId { get; set; }
    public InvoiceModel Model { get; set; }
    public FiscalEnvironment Environment { get; set; } = FiscalEnvironment.Homologation;
    public int Series { get; set; }
    public int Number { get; set; }
    public string? AccessKey { get; set; }
    public string? XmlPath { get; set; }
    public string? DanfePath { get; set; }
    public string? XmlSha256 { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string Provider { get; set; } = "NotConfigured";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ExternalDocumentId { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? Protocol { get; set; }
    public string? RejectionCode { get; set; }
    public string? RejectionMessage { get; set; }
    public bool IsContingency { get; set; }
    public DateTime? ContingencyStartedAt { get; set; }
    public Company? Company { get; set; }
    public Sale? Sale { get; set; }
    public ICollection<InvoiceEvent> Events { get; set; } = new List<InvoiceEvent>();
}
