using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NinaERP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("NinaERP.Domain.Entities.Client", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Document")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<string>("Email")
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Phone")
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<string>("Address")
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("CompanyId", "Document")
                    .IsUnique()
                    .HasName("IX_Clients_CompanyId_Document")
                    .HasFilter("[CompanyId] IS NOT NULL AND [Document] IS NOT NULL");

                b.ToTable("Clients", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Company", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Document")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<string>("Email")
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Phone")
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("Document")
                    .IsUnique()
                    .HasName("IX_Companies_Document");

                b.ToTable("Companies", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Product", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Description")
                    .HasMaxLength(1000)
                    .HasColumnType("nvarchar(1000)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<decimal>("Price")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.Property<string>("Sku")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("nvarchar(50)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("CompanyId", "Sku")
                    .IsUnique()
                    .HasName("IX_Products_CompanyId_Sku")
                    .HasFilter("[CompanyId] IS NOT NULL AND [Sku] IS NOT NULL");

                b.ToTable("Products", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Sale", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("ClientId")
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime>("SaleDate")
                    .HasColumnType("datetime2");

                b.Property<decimal>("TotalAmount")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("ClientId");

                b.HasIndex("CompanyId");

                b.ToTable("Sales", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.SaleItem", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("ProductId")
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("Quantity")
                    .HasColumnType("int");

                b.Property<Guid>("SaleId")
                    .HasColumnType("uniqueidentifier");

                b.Property<decimal>("TotalPrice")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.Property<decimal>("UnitPrice")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("ProductId");

                b.HasIndex("SaleId");

                b.ToTable("SaleItems", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Supplier", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<string>("Address")
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)");

                b.Property<string>("ContactPerson")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Document")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<bool>("IsActive")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("bit")
                    .HasDefaultValue(true);

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("Phone")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("nvarchar(20)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("CompanyId", "Document")
                    .IsUnique()
                    .HasName("IX_Suppliers_CompanyId_Document")
                    .HasFilter("[CompanyId] IS NOT NULL AND [Document] IS NOT NULL");

                b.ToTable("Suppliers", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.User", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid?>("CompanyId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("FullName")
                    .HasMaxLength(255)
                    .HasColumnType("nvarchar(255)");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Username")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique()
                    .HasName("IX_Users_Email");

                b.HasIndex("Username")
                    .IsUnique()
                    .HasName("IX_Users_Username");

                b.ToTable("Users", (string)null);
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Client", b =>
            {
                b.HasOne("NinaERP.Domain.Entities.Company", "Company")
                    .WithMany()
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.Navigation("Company");
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Product", b =>
            {
                b.HasOne("NinaERP.Domain.Entities.Company", "Company")
                    .WithMany()
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.Navigation("Company");
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Sale", b =>
            {
                b.HasOne("NinaERP.Domain.Entities.Client", "Client")
                    .WithMany()
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("NinaERP.Domain.Entities.Company", "Company")
                    .WithMany()
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.Navigation("Client");

                b.Navigation("Company");
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.SaleItem", b =>
            {
                b.HasOne("NinaERP.Domain.Entities.Product", "Product")
                    .WithMany()
                    .HasForeignKey("ProductId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("NinaERP.Domain.Entities.Sale", "Sale")
                    .WithMany("Items")
                    .HasForeignKey("SaleId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Product");

                b.Navigation("Sale");
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Supplier", b =>
            {
                b.HasOne("NinaERP.Domain.Entities.Company", "Company")
                    .WithMany()
                    .HasForeignKey("CompanyId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.Navigation("Company");
            });

            modelBuilder.Entity("NinaERP.Domain.Entities.Sale", b =>
            {
                b.Navigation("Items");
            });
        }
    }
}
