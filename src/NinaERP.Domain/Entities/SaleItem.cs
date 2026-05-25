using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => (Qty * UnitPrice) - Discount;
    public string Cfop { get; set; } = string.Empty;
    public decimal IcmsValue { get; set; }
    public decimal PisValue { get; set; }
    public decimal CofinsValue { get; set; }
    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
}
