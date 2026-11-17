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
        public DbSet<UserSession> UserSessions => Set<UserSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.TaxId).IsRequired().HasMaxLength(14);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.HasIndex(x => x.TaxId).IsUnique();
                entity.HasMany(x => x.Users).WithOne(x => x.Company).HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Username).IsRequired().HasMaxLength(100);
                entity.Property(x => x.NormalizedUsername).IsRequired().HasMaxLength(100);
                entity.Property(x => x.PasswordHash).IsRequired();
                entity.Property(x => x.FullName).HasMaxLength(200);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.SessionVersion).HasDefaultValue(0);
                entity.HasIndex(x => new { x.CompanyId, x.NormalizedUsername }).IsUnique();
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
                entity.Property(x => x.SessionVersion).IsRequired();
                entity.Property(x => x.RevocationReason).HasMaxLength(200);
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => new { x.CompanyId, x.RevokedAt, x.AbsoluteExpiresAt });
                entity.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
