using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
public sealed class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
        var mb = modelBuilder;

        mb.Entity<Company>().ToTable("Companies");
        mb.Entity<User>().ToTable("Users");
        mb.Entity<Department>().ToTable("Departments");
        mb.Entity<Product>().ToTable("Products");
        mb.Entity<ProductTax>().ToTable("ProductTaxes");
        mb.Entity<Client>().ToTable("Clients");
        mb.Entity<Supplier>().ToTable("Suppliers");
        mb.Entity<Sale>().ToTable("Sales");
        mb.Entity<SaleItem>().ToTable("SaleItems");
        mb.Entity<Payment>().ToTable("Payments");
        mb.Entity<PaymentTransaction>().ToTable("PaymentTransactions");
        mb.Entity<CashRegister>().ToTable("CashRegisters");
        mb.Entity<CashSession>().ToTable("CashSessions");
        mb.Entity<CashMovement>().ToTable("CashMovements");
        mb.Entity<Warehouse>().ToTable("Warehouses");
        mb.Entity<InventoryBalance>().ToTable("InventoryBalances");
        mb.Entity<InventoryReservation>().ToTable("InventoryReservations");
        mb.Entity<PurchaseOrder>().ToTable("PurchaseOrders");
        mb.Entity<PurchaseOrderItem>().ToTable("PurchaseOrderItems");
        mb.Entity<FinancialTitle>().ToTable("FinancialTitles");
        mb.Entity<FinancialSettlement>().ToTable("FinancialSettlements");
        mb.Entity<TaxProfile>().ToTable("TaxProfiles");
        mb.Entity<FiscalConfiguration>().ToTable("FiscalConfigurations");
        mb.Entity<InvoiceSeriesCounter>().ToTable("InvoiceSeriesCounters");
        mb.Entity<Invoice>().ToTable("Invoices");
        mb.Entity<InvoiceEvent>().ToTable("InvoiceEvents");
        mb.Entity<FiscalNumberVoid>().ToTable("FiscalNumberVoids");
        mb.Entity<TaxGuide>().ToTable("TaxGuides");
        mb.Entity<StockMovement>().ToTable("StockMovements");
        mb.Entity<IdempotencyRecord>().ToTable("IdempotencyRecords");
        mb.Entity<OutboxMessage>().ToTable("OutboxMessages");
        mb.Entity<AuditLog>().ToTable("AuditLogs");

        mb.Entity<Company>(e =>
        {
            e.HasIndex(x => x.Cnpj).IsUnique();
            e.Property(x => x.Cnpj).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TimeZoneId).HasMaxLength(100);
        });
        mb.Entity<User>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Username }).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<Department>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Products).WithOne(x => x.DepartmentNavigation).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });
        mb.Entity<Product>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Barcode });
            e.Property(x => x.Sku).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Ncm).HasMaxLength(8);
            e.Property(x => x.Cest).HasMaxLength(7);
            e.Property(x => x.PriceSale).HasPrecision(18, 2);
            e.Property(x => x.PriceMinimum).HasPrecision(18, 2);
            e.Property(x => x.CostAverage).HasPrecision(18, 4);
            e.Property(x => x.StockQty).HasPrecision(18, 4);
            e.Property(x => x.StockMin).HasPrecision(18, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Taxes).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<Client>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Document });
            e.Property(x => x.Document).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.CreditLimit).HasPrecision(18, 2);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<Supplier>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Document });
            e.Property(x => x.Document).HasMaxLength(18);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Warehouse>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.HasIndex(x => x.CompanyId).IsUnique().HasFilter("\"IsDefault\" = TRUE AND \"IsActive\" = TRUE");
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.BranchCode).HasMaxLength(30);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<InventoryBalance>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.ProductId }).IsUnique();
            e.Property(x => x.OnHand).HasPrecision(18, 4);
            e.Property(x => x.Reserved).HasPrecision(18, 4);
            e.Property(x => x.AverageCost).HasPrecision(18, 4);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Ignore(x => x.Available);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_InventoryBalance_OnHand", "\"OnHand\" >= 0");
                t.HasCheckConstraint("CK_InventoryBalance_Reserved", "\"Reserved\" >= 0 AND \"Reserved\" <= \"OnHand\"");
            });
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany(x => x.Balances).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });


        mb.Entity<InventoryReservation>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.SaleId, x.WarehouseId, x.ProductId }).IsUnique();
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.ReleasedQuantity).HasPrecision(18, 4);
            e.Property(x => x.ConsumedQuantity).HasPrecision(18, 4);
            e.Property(x => x.ReleaseReason).HasMaxLength(300);
            e.Ignore(x => x.ActiveQuantity);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_InventoryReservation_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint("CK_InventoryReservation_Released", "\"ReleasedQuantity\" >= 0");
                t.HasCheckConstraint("CK_InventoryReservation_Consumed", "\"ConsumedQuantity\" >= 0");
                t.HasCheckConstraint("CK_InventoryReservation_Total", "\"ReleasedQuantity\" + \"ConsumedQuantity\" <= \"Quantity\"");
            });
            e.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<CashRegister>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Sessions).WithOne(x => x.CashRegister).HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<CashSession>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.CashRegisterId, x.Status });
            e.HasIndex(x => new { x.CompanyId, x.UserId, x.Status });
            e.HasIndex(x => new { x.CompanyId, x.CashRegisterId }).IsUnique().HasFilter("\"Status\" = 0 AND \"IsActive\" = TRUE");
            e.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique().HasFilter("\"Status\" = 0 AND \"IsActive\" = TRUE");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.OpeningAmount).HasPrecision(18, 2);
            e.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
            e.Property(x => x.CountedAmount).HasPrecision(18, 2);
            e.Property(x => x.Difference).HasPrecision(18, 2);
            e.Property(x => x.ClosingNotes).HasMaxLength(500);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<CashMovement>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.CashSessionId, x.OccurredAt });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasOne(x => x.CashSession).WithMany(x => x.Movements).HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Sale).WithMany(x => x.CashMovements).HasForeignKey(x => x.SaleId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PaymentTransaction).WithMany(x => x.CashMovements).HasForeignKey(x => x.PaymentTransactionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Sale>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.SaleDate });
            e.HasIndex(x => new { x.CompanyId, x.Status });
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.CancellationReason).HasMaxLength(300);
            e.Property(x => x.TotalProducts).HasPrecision(18, 2);
            e.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            e.Property(x => x.TotalPaid).HasPrecision(18, 2);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CashSession).WithMany().HasForeignKey(x => x.CashSessionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payments).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PaymentTransactions).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Invoices).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<SaleItem>(e =>
        {
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.CostUnit).HasPrecision(18, 4);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.IcmsValue).HasPrecision(18, 2);
            e.Property(x => x.PisValue).HasPrecision(18, 2);
            e.Property(x => x.CofinsValue).HasPrecision(18, 2);
            e.Ignore(x => x.Total);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<Payment>(e => e.Property(x => x.Amount).HasPrecision(18, 2));
        mb.Entity<PaymentTransaction>(e =>
        {
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.CompanyId, x.SaleId });
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Provider, x.ExternalTransactionId }).IsUnique().HasFilter("\"ExternalTransactionId\" IS NOT NULL");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RefundedAmount).HasPrecision(18, 2);
            e.Property(x => x.Provider).HasMaxLength(80);
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.ExternalTransactionId).HasMaxLength(160);
            e.Property(x => x.Nsu).HasMaxLength(80);
            e.Property(x => x.AuthCode).HasMaxLength(80);
            e.Property(x => x.PixTxId).HasMaxLength(160);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PaymentTransactions_Amount", "\"Amount\" > 0");
                t.HasCheckConstraint("CK_PaymentTransactions_RefundedAmount", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"Amount\"");
            });
            e.HasOne(x => x.CashSession).WithMany().HasForeignKey(x => x.CashSessionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PurchaseOrder>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Status, x.OrderedAt });
            e.Property(x => x.Number).HasMaxLength(50);
            e.Property(x => x.TotalProducts).HasPrecision(18, 2);
            e.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            e.Property(x => x.TotalFreight).HasPrecision(18, 2);
            e.Property(x => x.TotalTaxes).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Payables).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<PurchaseOrderItem>(e =>
        {
            e.HasIndex(x => new { x.PurchaseOrderId, x.ProductId }).IsUnique();
            e.Property(x => x.OrderedQty).HasPrecision(18, 4);
            e.Property(x => x.ReceivedQty).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.Taxes).HasPrecision(18, 2);
            e.Ignore(x => x.Total);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<FinancialTitle>(e =>
        {
            e.ToTable("FinancialTitles");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasDiscriminator<string>("TitleKind")
                .HasValue<AccountPayable>("Payable")
                .HasValue<AccountReceivable>("Receivable");
            e.HasIndex(x => new { x.CompanyId, x.Status, x.DueDate });
            e.HasIndex(x => new { x.CompanyId, x.OriginType, x.OriginId });
            e.Property(x => x.DocumentNumber).HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(250);
            e.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            e.Property(x => x.OpenAmount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.OriginType).HasMaxLength(80);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Settlements).WithOne(x => x.Title).HasForeignKey(x => x.TitleId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<AccountPayable>(e =>
        {
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<AccountReceivable>(e =>
        {
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Sale).WithMany(x => x.Receivables).HasForeignKey(x => x.SaleId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<FinancialSettlement>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.TitleId, x.OccurredAt });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Interest).HasPrecision(18, 2);
            e.Property(x => x.Penalty).HasPrecision(18, 2);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.ExternalReference).HasMaxLength(160);
            e.Property(x => x.Notes).HasMaxLength(500);
        });

        mb.Entity<TaxProfile>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Name, x.VersionNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Status, x.EffectiveFrom, x.EffectiveTo });
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.EstablishmentCode).HasMaxLength(30);
            e.Property(x => x.OriginState).HasMaxLength(2);
            e.Property(x => x.DestinationState).HasMaxLength(2);
            e.Property(x => x.Ncm).HasMaxLength(8);
            e.Property(x => x.Cest).HasMaxLength(7);
            e.Property(x => x.Cfop).HasMaxLength(4);
            e.Property(x => x.Cst).HasMaxLength(4);
            e.Property(x => x.Csosn).HasMaxLength(4);
            e.Property(x => x.CClassTrib).HasMaxLength(20);
            e.Property(x => x.TaxBenefitCode).HasMaxLength(20);
            foreach (var prop in new[] { nameof(TaxProfile.IcmsRate), nameof(TaxProfile.IcmsStRate), nameof(TaxProfile.IcmsReductionRate), nameof(TaxProfile.IpiRate), nameof(TaxProfile.PisRate), nameof(TaxProfile.CofinsRate), nameof(TaxProfile.CbsRate), nameof(TaxProfile.IbsStateRate), nameof(TaxProfile.IbsMunicipalRate), nameof(TaxProfile.SelectiveTaxRate) })
                e.Property<decimal>(prop).HasPrecision(9, 4);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<FiscalConfiguration>(e =>
        {
            e.HasIndex(x => x.CompanyId).IsUnique();
            e.Property(x => x.Provider).HasMaxLength(80);
            e.Property(x => x.CertificateThumbprint).HasMaxLength(120);
            e.Property(x => x.ProviderSecretReference).HasMaxLength(240);
            e.Property(x => x.NfceCscId).HasMaxLength(20);
            e.Property(x => x.NfceCscSecretReference).HasMaxLength(240);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<InvoiceSeriesCounter>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Model, x.Series }).IsUnique();
            e.Property(x => x.Version).IsConcurrencyToken();
        });
        mb.Entity<Invoice>(e =>
        {
            e.HasIndex(x => x.AccessKey).IsUnique().HasFilter("\"AccessKey\" IS NOT NULL");
            e.HasIndex(x => new { x.CompanyId, x.Model, x.Series, x.Number }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.AccessKey).HasMaxLength(44);
            e.Property(x => x.Provider).HasMaxLength(80);
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.ExternalDocumentId).HasMaxLength(160);
            e.Property(x => x.Protocol).HasMaxLength(80);
            e.Property(x => x.RejectionCode).HasMaxLength(30);
            e.Property(x => x.RejectionMessage).HasMaxLength(500);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Events).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<InvoiceEvent>(e =>
        {
            e.HasIndex(x => new { x.InvoiceId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.ExternalEventId).HasMaxLength(160);
            e.Property(x => x.Protocol).HasMaxLength(80);
            e.Property(x => x.RejectionCode).HasMaxLength(30);
            e.Property(x => x.RejectionMessage).HasMaxLength(500);
        });

        mb.Entity<FiscalNumberVoid>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Model, x.Environment, x.Series, x.FromNumber, x.ToNumber });
            e.Property(x => x.Reason).HasMaxLength(255);
            e.Property(x => x.Provider).HasMaxLength(80);
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.Property(x => x.ExternalEventId).HasMaxLength(160);
            e.Property(x => x.Protocol).HasMaxLength(80);
            e.Property(x => x.RejectionCode).HasMaxLength(30);
            e.Property(x => x.RejectionMessage).HasMaxLength(500);
            e.Property(x => x.XmlPath).HasMaxLength(500);
            e.ToTable(t => t.HasCheckConstraint("CK_FiscalNumberVoid_Range", "\"FromNumber\" > 0 AND \"ToNumber\" >= \"FromNumber\""));
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<TaxGuide>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.ReferenceYear, x.ReferenceMonth, x.TaxType });
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestAmount).HasPrecision(18, 2);
            e.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxType).HasMaxLength(40);
            e.Property(x => x.RevenueCode).HasMaxLength(60);
            e.Property(x => x.DigitableLine).HasMaxLength(200);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AccountPayable).WithMany().HasForeignKey(x => x.AccountPayableId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<StockMovement>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.ProductId, x.MovedAt });
            e.HasIndex(x => new { x.CompanyId, x.OriginType, x.OriginId });
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.CostUnit).HasPrecision(18, 4);
            e.Property(x => x.OriginType).HasMaxLength(80);
            e.Property(x => x.IdempotencyKey).HasMaxLength(120);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CounterpartyWarehouse).WithMany().HasForeignKey(x => x.CounterpartyWarehouseId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<IdempotencyRecord>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.Scope, x.Key }).IsUnique();
            e.Property(x => x.Scope).HasMaxLength(80);
            e.Property(x => x.Key).HasMaxLength(120);
            e.Property(x => x.RequestHash).HasMaxLength(1024);
        });
        mb.Entity<OutboxMessage>(e =>
        {
            e.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt });
            e.HasIndex(x => new { x.CompanyId, x.AggregateType, x.AggregateId });
            e.Property(x => x.Type).HasMaxLength(120);
            e.Property(x => x.AggregateType).HasMaxLength(80);
            e.Property(x => x.LastError).HasMaxLength(1000);
        });
        mb.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => new { x.CompanyId, x.OccurredAt });
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.Method).HasMaxLength(10);
            e.Property(x => x.Path).HasMaxLength(500);
            e.Property(x => x.RemoteIp).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
        });


    
    }
}
