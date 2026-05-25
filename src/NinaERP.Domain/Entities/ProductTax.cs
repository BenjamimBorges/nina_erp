using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public class ProductTax : BaseEntity
{
    public Guid ProductId { get; set; }
    public string UfDest { get; set; } = "MT";
    public string Cfop { get; set; } = string.Empty;
    public int IcmsCst { get; set; }
    public decimal IcmsAliq { get; set; }
    public decimal IcmsRedbc { get; set; }
    public int PisCst { get; set; }
    public decimal PisAliq { get; set; }
    public int CofinsCst { get; set; }
    public decimal CofinsAliq { get; set; }
    public bool HasIpi { get; set; }
    public decimal IpiAliq { get; set; }
    public Product? Product { get; set; }
}
