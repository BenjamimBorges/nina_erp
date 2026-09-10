using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class FiscalNumberVoid : BaseEntity
{
    public Guid CompanyId { get; set; }
    public InvoiceModel Model { get; set; }
    public FiscalEnvironment Environment { get; set; }
    public int Series { get; set; }
    public int FromNumber { get; set; }
    public int ToNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public FiscalNumberVoidStatus Status { get; set; } = FiscalNumberVoidStatus.Pending;
    public string Provider { get; set; } = "NotConfigured";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public string? Protocol { get; set; }
    public string? RejectionCode { get; set; }
    public string? RejectionMessage { get; set; }
    public string? XmlPath { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Company? Company { get; set; }
}
