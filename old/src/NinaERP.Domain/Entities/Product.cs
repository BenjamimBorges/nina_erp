using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Product : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Sku { get; set; } = string.Empty; // UK
    public string Name { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string Cest { get; set; } = string.Empty;
    public string Unit { get; set; } = "UN";
    public decimal PriceSale { get; set; }
    public decimal PriceCost { get; set; }
    public decimal CostAverage { get; set; }
    public int StockQty { get; set; }
    public int StockMin { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public ICollection<ProductTax> Taxes { get; set; } = new List<ProductTax>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}
