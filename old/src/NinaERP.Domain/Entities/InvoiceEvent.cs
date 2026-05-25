using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class InvoiceEvent : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public string EventType { get; set; } = string.Empty; // Authorization, Cancellation, etc
    public string XmlPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
