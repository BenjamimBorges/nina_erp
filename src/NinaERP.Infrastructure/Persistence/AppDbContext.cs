using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTax> ProductTaxes => Set<ProductTax>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceEvent> InvoiceEvents => Set<InvoiceEvent>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Companies
        mb.Entity<Company>(e => {
            e.HasIndex(x => x.Cnpj).IsUnique();
            e.Property(x => x.Cnpj).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
        });

        // Users
        mb.Entity<User>(e => {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });

        // Products
        mb.Entity<Product>(e => {
            e.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Ncm).HasMaxLength(8);
            e.Property(x => x.Cest).HasMaxLength(7);
            e.Property(x => x.PriceSale).HasColumnType("decimal(18,2)");
            e.Property(x => x.PriceMinimum).HasColumnType("decimal(18,2)");
            e.Property(x => x.CostAverage).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasMany(x => x.Taxes).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
        });

        // Clients
        mb.Entity<Client>(e => {
            e.HasIndex(x => new { x.CompanyId, x.Document });
            e.Property(x => x.Document).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.CreditLimit).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });

        // Suppliers
        mb.Entity<Supplier>(e => {
            e.HasIndex(x => new { x.CompanyId, x.Document });
            e.Property(x => x.Document).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });

        // Sales
        mb.Entity<Sale>(e => {
            e.Property(x => x.TotalProducts).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalDiscount).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPaid).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).IsRequired(false);
            e.HasMany(x => x.Items).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId);
            e.HasMany(x => x.Payments).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId);
            e.HasOne(x => x.Invoice).WithOne(x => x.Sale).HasForeignKey<Invoice>(x => x.SaleId);
        });

        // SaleItems
        mb.Entity<SaleItem>(e => {
            e.Property(x => x.Qty).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Ignore(x => x.Total);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        // Payments
        mb.Entity<Payment>(e => {
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        });

        // Invoices
        mb.Entity<Invoice>(e => {
            e.HasIndex(x => x.AccessKey).IsUnique().HasFilter("\"AccessKey\" IS NOT NULL");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasMany(x => x.Events).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId);
        });

        // StockMovements
        mb.Entity<StockMovement>(e => {
            e.Property(x => x.Qty).HasColumnType("decimal(18,4)");
            e.Property(x => x.CostUnit).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        });

        base.OnModelCreating(mb);
    }
}
