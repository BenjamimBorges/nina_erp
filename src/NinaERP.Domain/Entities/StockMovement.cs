using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;
public class StockMovement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Qty { get; set; }
    public decimal CostUnit { get; set; }
    public string OriginType { get; set; } = string.Empty;
    public Guid? OriginId { get; set; }
    public string? Notes { get; set; }
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;
    public Company? Company { get; set; }
    public Product? Product { get; set; }
}
