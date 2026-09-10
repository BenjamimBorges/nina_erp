using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;
using NinaERP.Infrastructure.Services;
using Npgsql;
using Xunit;

namespace NinaERP.Tests;

[Trait("Category", "Integration")]
[Collection("Postgres")]
public sealed class PostgresIntegrationTests
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("NINAERP_TEST_CONNECTION")
        ?? throw new InvalidOperationException("NINAERP_TEST_CONNECTION é obrigatório para testes de integração PostgreSQL.");

    private static AppDbContext CreateDb(string? connectionString = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString ?? ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Migrations_Create_Commercial_Schema_And_Reporting_Views()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        var requiredTables = new[]
        {
            "CashRegisters", "CashSessions", "CashMovements", "PaymentTransactions",
            "Warehouses", "InventoryBalances", "InventoryReservations", "PurchaseOrders",
            "FinancialTitles", "TaxProfiles", "FiscalConfigurations", "FiscalNumberVoids",
            "TaxGuides", "IdempotencyRecords", "OutboxMessages", "ApprovalRequests",
            "ChannelOrders", "ChannelOrderItems", "AssistantInteractions", "DistributedLeases", "SystemNodes"
        };
        foreach (var table in requiredTables)
            Assert.True(await RelationExistsAsync(db, table, "BASE TABLE"), $"Tabela {table} ausente após migration.");

        var requiredViews = new[]
        {
            "vw_sales_daily", "vw_seller_sales", "vw_product_sales", "vw_inventory_balances",
            "vw_stock_movements", "vw_cash_sessions", "vw_payments", "vw_accounts_payable",
            "vw_accounts_receivable", "vw_fiscal_documents", "vw_tax_guides", "vw_financial_summary",
            "vw_bi_seller_monthly", "vw_bi_payment_mix_monthly"
        };
        foreach (var view in requiredViews)
            Assert.True(await RelationExistsAsync(db, view, "VIEW"), $"View {view} ausente após migration.");
    }

    [Fact]
    public async Task Inventory_Check_Constraints_Reject_OverReserved_Stock()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var company = NewCompany();
        var warehouse = new Warehouse { CompanyId = company.Id, Code = $"T{Guid.NewGuid():N}"[..20], Name = "Teste", IsDefault = false };
        var product = NewProduct(company.Id);
        db.AddRange(company, warehouse, product);
        await db.SaveChangesAsync();

        db.InventoryBalances.Add(new InventoryBalance
        {
            CompanyId = company.Id, WarehouseId = warehouse.Id, ProductId = product.Id,
            OnHand = 1, Reserved = 2, AverageCost = 1
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Cash_Register_Rejects_Two_Open_Sessions_Concurrently()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var company = NewCompany();
        var register = new CashRegister { CompanyId = company.Id, Code = $"C{Guid.NewGuid():N}"[..20], Name = "Caixa teste" };
        db.AddRange(company, register);
        await db.SaveChangesAsync();

        db.CashSessions.Add(new CashSession { CompanyId = company.Id, CashRegisterId = register.Id, UserId = Guid.NewGuid(), OpeningAmount = 0, ExpectedAmount = 0 });
        await db.SaveChangesAsync();
        db.CashSessions.Add(new CashSession { CompanyId = company.Id, CashRegisterId = register.Id, UserId = Guid.NewGuid(), OpeningAmount = 0, ExpectedAmount = 0 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Reporting_Views_Expose_CompanyId()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var views = new[] { "vw_sales_daily", "vw_seller_sales", "vw_product_sales", "vw_inventory_balances", "vw_cash_sessions", "vw_payments", "vw_accounts_payable", "vw_accounts_receivable", "vw_fiscal_documents", "vw_tax_guides" };
        await db.Database.OpenConnectionAsync();
        foreach (var view in views)
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='public' AND table_name=@v AND column_name='CompanyId'";
            var p = cmd.CreateParameter(); p.ParameterName = "v"; p.Value = view; cmd.Parameters.Add(p);
            Assert.Equal(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
        }
    }

    [Fact]
    public async Task Cash_Payment_Updates_Live_Expected_Amount_And_Cash_Refund_Uses_Open_Session()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        var company = NewCompany();
        var userId = Guid.NewGuid();
        var register = new CashRegister { CompanyId = company.Id, Code = $"C{Guid.NewGuid():N}"[..20], Name = "Caixa fluxo" };
        var warehouse = new Warehouse { CompanyId = company.Id, Code = $"W{Guid.NewGuid():N}"[..20], Name = "Depósito padrão", IsDefault = true };
        var product = NewProduct(company.Id);
        product.StockQty = 10m;
        db.AddRange(company, register, warehouse, product);
        await db.SaveChangesAsync();

        var cash = new CashManagementService(db, new ApprovalService(db));
        var session = await cash.OpenSessionAsync(company.Id, userId, register.Id, 100m, $"open-{Guid.NewGuid():N}", CancellationToken.None);

        var sale = new Sale
        {
            CompanyId = company.Id, UserId = userId, WarehouseId = warehouse.Id,
            TotalProducts = 20m, TotalDiscount = 0m, TotalPaid = 0m, Status = SaleStatus.Open
        };
        sale.Items.Add(new SaleItem
        {
            SaleId = sale.Id, ProductId = product.Id, Product = product, Qty = 2m, UnitPrice = 10m, CostUnit = 5m
        });
        db.Sales.Add(sale);
        db.InventoryBalances.Add(new InventoryBalance
        {
            CompanyId = company.Id, WarehouseId = warehouse.Id, ProductId = product.Id,
            OnHand = 10m, Reserved = 0m, AverageCost = 5m
        });
        await db.SaveChangesAsync();

        var payments = new PaymentService(db);
        var registered = await payments.RegisterAsync(
            company.Id, userId, sale.Id, session.Id,
            [new PaymentRequestDto(PaymentMethod.Cash, 20m, $"pay-{Guid.NewGuid():N}")],
            CancellationToken.None);

        Assert.Single(registered);
        Assert.Equal(PaymentTransactionStatus.Captured, registered[0].Status);
        db.ChangeTracker.Clear();
        var afterSale = await db.CashSessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        Assert.Equal(120m, afterSale.ExpectedAmount);
        Assert.Equal(SaleStatus.Completed, await db.Sales.Where(x => x.Id == sale.Id).Select(x => x.Status).SingleAsync());

        await payments.RefundAsync(company.Id, userId, registered[0].Id, 5m, $"refund-{Guid.NewGuid():N}", CancellationToken.None);
        db.ChangeTracker.Clear();
        var afterRefund = await db.CashSessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        Assert.Equal(115m, afterRefund.ExpectedAmount);
        Assert.True(await db.CashMovements.AnyAsync(x => x.PaymentTransactionId == registered[0].Id && x.Type == CashMovementType.Refund && x.CashSessionId == session.Id));
    }

    [Fact]
    public async Task Commercial_Schema_Allows_Same_Username_In_Different_Companies()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var first = NewCompany();
        var second = NewCompany();
        db.AddRange(first, second);
        await db.SaveChangesAsync();

        var username = $"shared-{Guid.NewGuid():N}"[..24];
        db.Users.AddRange(
            new User { CompanyId = first.Id, Username = username, PasswordHash = "test", FullName = "Usuário 1", Email = "u1@example.invalid", Role = UserRole.Cashier },
            new User { CompanyId = second.Id, Username = username, PasswordHash = "test", FullName = "Usuário 2", Email = "u2@example.invalid", Role = UserRole.Cashier });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Users.CountAsync(x => x.Username == username));
    }

    [Fact]
    public async Task Commercial_Schema_Allows_Multiple_Fiscal_Documents_For_One_Sale()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var company = NewCompany();
        var sale = new Sale { CompanyId = company.Id, UserId = Guid.NewGuid(), TotalProducts = 1m, Status = SaleStatus.Open };
        db.AddRange(company, sale);
        await db.SaveChangesAsync();

        db.Invoices.AddRange(
            new Invoice { CompanyId = company.Id, SaleId = sale.Id, Model = InvoiceModel.NFeModel55, Series = 1, Number = Random.Shared.Next(100000, 200000), Status = InvoiceStatus.Cancelled, IdempotencyKey = $"invoice-{Guid.NewGuid():N}" },
            new Invoice { CompanyId = company.Id, SaleId = sale.Id, Model = InvoiceModel.NFeModel55, Series = 1, Number = Random.Shared.Next(200001, 300000), Status = InvoiceStatus.Draft, IdempotencyKey = $"invoice-{Guid.NewGuid():N}" });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Invoices.CountAsync(x => x.SaleId == sale.Id));
    }

    [Fact]
    public async Task Legacy_Baseline_Upgrade_Preserves_Data_And_Remaps_Enums()
    {
        var source = new NpgsqlConnectionStringBuilder(ConnectionString);
        var dbName = $"nina_upgrade_{Guid.NewGuid():N}";
        var admin = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
        await using (var adminConnection = new NpgsqlConnection(admin.ConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", adminConnection);
            await create.ExecuteNonQueryAsync();
        }

        var isolated = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = dbName, Pooling = false };
        try
        {
            await using (var db = CreateDb(isolated.ConnectionString))
            {
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync("20260910090000_InitialLegacySchema");
                var companyId = Guid.NewGuid();
                var productId = Guid.NewGuid();
                var saleId = Guid.NewGuid();
                var invoiceId = Guid.NewGuid();
                await db.Database.OpenConnectionAsync();
                await using var cmd = db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = """
INSERT INTO "Companies" ("Id","Cnpj","Name","FantasyName","StateRegistration","MunicipalRegistration","Address","City","State","ZipCode","Phone","Email","FiscalRegime","NfeSeries","NfceSeries","NfeNumber","NfceNumber","CreatedAt","UpdatedAt","IsActive")
VALUES (@company,'12345678000199','Legacy','Legacy','','','','','SP','','','',1,1,1,0,0,NOW(),NOW(),TRUE);
INSERT INTO "Products" ("Id","CompanyId","Sku","Name","Description","Ncm","Cest","Unit","PriceSale","PriceMinimum","CostAverage","StockQty","StockMin","Version","Brand","Department","Barcode","CreatedAt","UpdatedAt","IsActive")
VALUES (@product,@company,'LEG-1','Produto legado','','','','UN',10,0,4,7,1,0,'','','',NOW(),NOW(),TRUE);
INSERT INTO "Sales" ("Id","CompanyId","UserId","TotalProducts","TotalDiscount","TotalPaid","Status","SaleDate","CreatedAt","UpdatedAt","IsActive")
VALUES (@sale,@company,@user,10,0,10,1,NOW(),NOW(),NOW(),TRUE);
INSERT INTO "Invoices" ("Id","CompanyId","SaleId","Model","Series","Number","Status","CreatedAt","UpdatedAt","IsActive")
VALUES (@invoice,@company,@sale,55,1,1,2,NOW(),NOW(),TRUE);
""";
                Add(cmd, "company", companyId); Add(cmd, "product", productId); Add(cmd, "sale", saleId); Add(cmd, "invoice", invoiceId); Add(cmd, "user", Guid.NewGuid());
                await cmd.ExecuteNonQueryAsync();
                await db.Database.CloseConnectionAsync();
                await migrator.MigrateAsync();

                var saleStatus = await ScalarAsync<int>(db, "SELECT \"Status\" FROM \"Sales\" WHERE \"Id\"=@id", saleId);
                var invoiceStatus = await ScalarAsync<int>(db, "SELECT \"Status\" FROM \"Invoices\" WHERE \"Id\"=@id", invoiceId);
                var balance = await ScalarAsync<decimal>(db, "SELECT \"OnHand\" FROM \"InventoryBalances\" WHERE \"ProductId\"=@id", productId);
                Assert.Equal((int)SaleStatus.Completed, saleStatus);
                Assert.Equal((int)InvoiceStatus.Authorized, invoiceStatus);
                Assert.Equal(7m, balance);
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var adminConnection = new NpgsqlConnection(admin.ConnectionString);
            await adminConnection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\" WITH (FORCE)", adminConnection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static Company NewCompany() => new()
    {
        Cnpj = $"9{Random.Shared.NextInt64(1000000000000, 9999999999999)}",
        Name = "Empresa teste",
        FiscalRegime = FiscalRegime.SimplesNacional,
        State = "SP"
    };

    private static Product NewProduct(Guid companyId) => new()
    {
        CompanyId = companyId,
        Sku = $"SKU-{Guid.NewGuid():N}"[..24],
        Name = "Produto teste",
        Unit = "UN",
        PriceSale = 10,
        CostAverage = 5,
        StockQty = 0,
        StockMin = 0
    };

    private static async Task<bool> RelationExistsAsync(AppDbContext db, string name, string type)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name=@n AND table_type=@t";
        Add(cmd, "n", name); Add(cmd, "t", type);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
    }

    private static async Task<T> ScalarAsync<T>(AppDbContext db, string sql, Guid id)
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql; Add(cmd, "id", id);
        return (T)Convert.ChangeType((await cmd.ExecuteScalarAsync())!, typeof(T));
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p);
    }
}
