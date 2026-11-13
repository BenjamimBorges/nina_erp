using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ERP.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ERP.Core.Entities.Company", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer");
                b.Property<string>("Address").IsRequired().HasColumnType("text");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("boolean").HasDefaultValue(true);
                b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("TaxId").IsRequired().HasMaxLength(14).HasColumnType("character varying(14)");
                b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone");
                b.HasKey("Id");
                b.HasIndex("TaxId").IsUnique();
                b.ToTable("Companies");
            });

            modelBuilder.Entity("ERP.Core.Entities.User", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("FullName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("boolean").HasDefaultValue(true);
                b.Property<string>("NormalizedUsername").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
                b.Property<string>("PasswordHash").IsRequired().HasColumnType("text");
                b.Property<int>("Role").HasColumnType("integer");
                b.Property<int>("SessionVersion").ValueGeneratedOnAdd().HasColumnType("integer").HasDefaultValue(0);
                b.Property<string>("Username").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
                b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone");
                b.Property<int>("CompanyId").HasColumnType("integer");
                b.HasKey("Id");
                b.HasIndex("CompanyId", "NormalizedUsername").IsUnique();
                b.ToTable("Users");
            });

            modelBuilder.Entity("ERP.Core.Entities.UserSession", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<DateTime>("AbsoluteExpiresAt").HasColumnType("timestamp with time zone");
                b.Property<int>("CompanyId").HasColumnType("integer");
                b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<DateTime>("LastActivityAt").HasColumnType("timestamp with time zone");
                b.Property<string>("RevocationReason").HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<DateTime?>("RevokedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("TokenHash").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)");
                b.Property<int>("UserId").HasColumnType("integer");
                b.HasKey("Id");
                b.HasIndex("CompanyId", "RevokedAt", "AbsoluteExpiresAt");
                b.HasIndex("TokenHash").IsUnique();
                b.HasIndex("UserId");
                b.ToTable("UserSessions");
            });

            modelBuilder.Entity("ERP.Core.Entities.User", b =>
            {
                b.HasOne("ERP.Core.Entities.Company", "Company")
                    .WithMany("Users")
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
                b.Navigation("Company");
            });

            modelBuilder.Entity("ERP.Core.Entities.UserSession", b =>
            {
                b.HasOne("ERP.Core.Entities.Company", "Company")
                    .WithMany()
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
                b.HasOne("ERP.Core.Entities.User", "User")
                    .WithMany("Sessions")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
                b.Navigation("Company");
                b.Navigation("User");
            });

            modelBuilder.Entity("ERP.Core.Entities.Company", b =>
            {
                b.Navigation("Users");
            });

            modelBuilder.Entity("ERP.Core.Entities.User", b =>
            {
                b.Navigation("Sessions");
            });
#pragma warning restore 612, 618
        }
    }
}
