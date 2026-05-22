using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Data;

public class NinaErpDbContext : DbContext
{
    public NinaErpDbContext(DbContextOptions<NinaErpDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTax> ProductTaxes => Set<ProductTax>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceEvent> InvoiceEvents => Set<InvoiceEvent>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Cnpj).IsRequired().HasMaxLength(14);
            b.HasIndex(x => x.Cnpj).IsUnique();
            b.Property(x => x.Ie).HasMaxLength(20);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.FantasyName).HasMaxLength(255);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.CertPfxPath).HasMaxLength(500);
            b.Property(x => x.CertPasswordHash).HasMaxLength(255);
            b.HasMany(x => x.Users).WithOne(u => u.Company).HasForeignKey(u => u.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Clients).WithOne(c => c.Company).HasForeignKey(c => c.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Products).WithOne(p => p.Company).HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Sales).WithOne(s => s.Company).HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Purchases).WithOne(p => p.Company).HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Invoices).WithOne(i => i.Company).HasForeignKey(i => i.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.StockMovements).WithOne(sm => sm.Company).HasForeignKey(sm => sm.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Client
        modelBuilder.Entity<Client>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Document).IsRequired().HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(255);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.Address).HasMaxLength(500);
            b.HasOne(x => x.Company).WithMany(c => c.Clients).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.CompanyId, x.Document }).IsUnique();
        });

        // Product
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(50);
            b.HasIndex(x => x.Sku).IsUnique();
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Ncm).HasMaxLength(8);
            b.Property(x => x.Cest).HasMaxLength(7);
            b.Property(x => x.Unit).IsRequired().HasMaxLength(5).HasDefaultValue("UN");
            b.Property(x => x.PriceSale).HasPrecision(18, 2);
            b.Property(x => x.PriceCost).HasPrecision(18, 2);
            b.Property(x => x.CostAverage).HasPrecision(18, 2);
            b.Property(x => x.Brand).HasMaxLength(100);
            b.Property(x => x.Department).HasMaxLength(100);
            b.HasOne(x => x.Company).WithMany(c => c.Products).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Taxes).WithOne(pt => pt.Product).HasForeignKey(pt => pt.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.StockMovements).WithOne(sm => sm.Product).HasForeignKey(sm => sm.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.SaleItems).WithOne(si => si.Product).HasForeignKey(si => si.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.PurchaseItems).WithOne(pi => pi.Product).HasForeignKey(pi => pi.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // ProductTax
        modelBuilder.Entity<ProductTax>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UfDest).HasMaxLength(2);
            b.Property(x => x.Cfop).HasMaxLength(5);
            b.Property(x => x.IcmsAliq).HasPrecision(5, 2);
            b.Property(x => x.PisAliq).HasPrecision(5, 2);
            b.Property(x => x.CofinsAliq).HasPrecision(5, 2);
            b.HasOne(x => x.Product).WithMany(p => p.Taxes).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Supplier
        modelBuilder.Entity<Supplier>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Document).IsRequired().HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(255);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.ContactPerson).HasMaxLength(255);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.CompanyId, x.Document }).IsUnique();
            b.HasMany(x => x.Purchases).WithOne(p => p.Supplier).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).IsRequired().HasMaxLength(100);
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.Email).IsRequired().HasMaxLength(255);
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(255);
            b.HasOne(x => x.Company).WithMany(c => c.Users).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Sale
        modelBuilder.Entity<Sale>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalProducts).HasPrecision(18, 2);
            b.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            b.Property(x => x.TotalPaid).HasPrecision(18, 2);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
            b.Property(x => x.FiscalModel).IsRequired().HasMaxLength(10).HasDefaultValue("NFe");
            b.Property(x => x.SaleDate).IsRequired();
            b.HasOne(x => x.Company).WithMany(c => c.Sales).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Items).WithOne(si => si.Sale).HasForeignKey(si => si.SaleId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Payments).WithOne(p => p.Sale).HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        // SaleItem
        modelBuilder.Entity<SaleItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Qty).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.Cfop).HasMaxLength(5);
            b.Property(x => x.IcmsValue).HasPrecision(18, 2);
            b.HasOne(x => x.Sale).WithMany(s => s.Items).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany(p => p.SaleItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Purchase
        modelBuilder.Entity<Purchase>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalProducts).HasPrecision(18, 2);
            b.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            b.Property(x => x.TotalPaid).HasPrecision(18, 2);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
            b.Property(x => x.PurchaseDate).IsRequired();
            b.HasOne(x => x.Company).WithMany(c => c.Purchases).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Supplier).WithMany(s => s.Purchases).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Items).WithOne(pi => pi.Purchase).HasForeignKey(pi => pi.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseItem
        modelBuilder.Entity<PurchaseItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Qty).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.HasOne(x => x.Purchase).WithMany(p => p.Items).HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany(p => p.PurchaseItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Model).IsRequired().HasMaxLength(10);
            b.Property(x => x.AccessKey).IsRequired().HasMaxLength(44);
            b.HasIndex(x => x.AccessKey).IsUnique();
            b.Property(x => x.XmlPath).HasMaxLength(500);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
            b.Property(x => x.CancelReason).HasMaxLength(500);
            b.HasOne(x => x.Company).WithMany(c => c.Invoices).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Sale).WithOne(s => s.Invoice).HasForeignKey<Invoice>(x => x.SaleId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Purchase).WithOne(p => p.Invoice).HasForeignKey<Invoice>(x => x.PurchaseId).OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.Events).WithOne(ie => ie.Invoice).HasForeignKey(ie => ie.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // InvoiceEvent
        modelBuilder.Entity<InvoiceEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired().HasMaxLength(50);
            b.Property(x => x.XmlPath).HasMaxLength(500);
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
            b.Property(x => x.OccurredAt).IsRequired();
            b.HasOne(x => x.Invoice).WithMany(i => i.Events).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).IsRequired().HasMaxLength(50);
            b.Property(x => x.Qty).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.CostUnit).HasPrecision(18, 2);
            b.Property(x => x.OriginType).IsRequired().HasMaxLength(50);
            b.Property(x => x.MovedAt).IsRequired();
            b.HasOne(x => x.Company).WithMany(c => c.StockMovements).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany(p => p.StockMovements).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Payment
        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Method).IsRequired().HasMaxLength(50);
            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Nsu).HasMaxLength(20);
            b.Property(x => x.AuthCode).HasMaxLength(20);
            b.Property(x => x.PixTxId).HasMaxLength(50);
            b.HasOne(x => x.Sale).WithMany(s => s.Payments).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

