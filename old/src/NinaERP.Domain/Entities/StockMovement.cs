using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string Type { get; set; } = string.Empty; // Entry, Exit, Adjustment
    public decimal Qty { get; set; }
    public decimal CostUnit { get; set; }
    public string OriginType { get; set; } = string.Empty; // Sale, Purchase, Adjustment
    public Guid OriginId { get; set; }
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;
}
