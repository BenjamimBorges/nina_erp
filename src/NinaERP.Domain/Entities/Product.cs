using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;

public class Product : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string Cest { get; set; } = string.Empty;
    public string Unit { get; set; } = "UN";
    public decimal PriceSale { get; set; }
    public decimal PriceMinimum { get; set; }
    public decimal CostAverage { get; set; }
    public int StockQty { get; set; }
    public int StockMin { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public Company? Company { get; set; }
    public Department? DepartmentNavigation { get; set; }
    public ICollection<ProductTax> Taxes { get; set; } = new List<ProductTax>();
}

