using ERP.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Stock> Stocks => Set<Stock>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasMany(x => x.Users).WithOne(x => x.Company).HasForeignKey(x => x.CompanyId);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(x => x.Username).IsRequired().HasMaxLength(100);
                entity.Property(x => x.PasswordHash).IsRequired();
                entity.Property(x => x.FullName).HasMaxLength(200);
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Document).HasMaxLength(50);
                entity.HasOne(x => x.Company).WithMany(x => x.Clients).HasForeignKey(x => x.CompanyId);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Sku).HasMaxLength(100);
                entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
                entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
            });

            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
                entity.Property(x => x.Location).HasMaxLength(100);
            });
        }
    }
}
