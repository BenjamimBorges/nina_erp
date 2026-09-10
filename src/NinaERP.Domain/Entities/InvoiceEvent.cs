using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;
public class InvoiceEvent : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public FiscalEventType Type { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? XmlPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Protocol { get; set; }
    public string? ExternalEventId { get; set; }
    public string? RejectionCode { get; set; }
    public string? RejectionMessage { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Invoice? Invoice { get; set; }
}
