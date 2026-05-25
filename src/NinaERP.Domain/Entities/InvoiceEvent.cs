using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public class InvoiceEvent : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? XmlPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Protocol { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Invoice? Invoice { get; set; }
}
