using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public class Company : BaseEntity
{
    public string Cnpj { get; set; } = string.Empty; // UK
    public string Ie { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FantasyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string CertPfxPath { get; set; } = string.Empty;
    public string CertPasswordHash { get; set; } = string.Empty;
    public int FiscalRegime { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
