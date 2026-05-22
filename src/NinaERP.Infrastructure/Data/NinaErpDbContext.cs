using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Data;

public class NinaErpDbContext : DbContext
{
    public NinaErpDbContext(DbContextOptions<NinaErpDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Document).IsRequired().HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(255);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.HasIndex(x => x.Document).IsUnique();
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
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.CompanyId, x.Document }).IsUnique();
        });

        // Product
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Sku).IsRequired().HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
        });

        // Supplier
        modelBuilder.Entity<Supplier>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(255);
            b.Property(x => x.Document).IsRequired().HasMaxLength(20);
            b.Property(x => x.Email).IsRequired().HasMaxLength(255);
            b.Property(x => x.Phone).IsRequired().HasMaxLength(20);
            b.Property(x => x.Address).IsRequired().HasMaxLength(500);
            b.Property(x => x.ContactPerson).IsRequired().HasMaxLength(255);
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.CompanyId, x.Document }).IsUnique();
        });

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).IsRequired().HasMaxLength(100);
            b.Property(x => x.Email).IsRequired().HasMaxLength(255);
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(255);
            b.HasIndex(x => x.Username).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
        });

        // Sale
        modelBuilder.Entity<Sale>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.SaleDate).IsRequired();
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        // SaleItem
        modelBuilder.Entity<SaleItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.TotalPrice).HasPrecision(18, 2).IsRequired();
            b.HasOne(x => x.Sale).WithMany(s => s.Items).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
