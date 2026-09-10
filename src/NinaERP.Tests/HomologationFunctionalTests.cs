using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;
using NinaERP.Infrastructure.Persistence.Repositories;
using NinaERP.Infrastructure.Services;
using Xunit;

namespace NinaERP.Tests;

[Trait("Category", "Integration")]
[Collection("Postgres")]
public sealed class HomologationFunctionalTests
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("NINAERP_TEST_CONNECTION")
        ?? throw new InvalidOperationException("NINAERP_TEST_CONNECTION é obrigatório para testes de integração PostgreSQL.");

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Homologation_Seed_Creates_Requested_Users_Products_Clients_Sellers_And_Sales()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var old = Environment.GetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable);
        Environment.SetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable, $"Hml!{Guid.NewGuid():N}Aa1");
        try
        {
            var summary = await HomologationSeeder.SeedAsync(db);
            Assert.Equal(6, summary.Users);
            Assert.Equal(20, summary.Products);
            Assert.Equal(4, summary.Clients);
            Assert.Equal(3, summary.Sellers);
            Assert.Equal(5, summary.Sales);

            var users = await db.Users.AsNoTracking().Where(x => x.CompanyId == summary.CompanyId).ToListAsync();
            Assert.Contains(users, x => x.Username == "admin_hml" && x.Role == UserRole.Admin);
            Assert.Contains(users, x => x.Username == "gerente_hml" && x.Role == UserRole.Manager);
            Assert.Contains(users, x => x.Username == "estoque_hml" && x.Role == UserRole.Stock);
            Assert.Equal(3, users.Count(x => HomologationSeeder.Sellers.Contains(x.Username) && x.Role == UserRole.Cashier));

            var products = await db.Products.AsNoTracking()
                .Where(x => x.CompanyId == summary.CompanyId && x.Sku.StartsWith("HML-"))
                .OrderBy(x => x.Sku).ToListAsync();
            Assert.Equal(20, products.Count);
            Assert.Equal(20, products.Select(x => x.Barcode).Distinct().Count());
            Assert.All(products, x => Assert.True(x.PriceSale > x.CostAverage, $"{x.Sku}: preço de venda deve superar custo de homologação."));
            Assert.All(products, x => Assert.Matches("^[0-9]{13}$", x.Barcode));

            var purchase = await db.PurchaseOrders.AsNoTracking().Include(x => x.Items)
                .SingleAsync(x => x.CompanyId == summary.CompanyId && x.Number == "HML-PO-001");
            Assert.Equal(PurchaseOrderStatus.Received, purchase.Status);
            Assert.Equal(20, purchase.Items.Count);
            Assert.All(purchase.Items, x => Assert.Equal(100m, x.OrderedQty));
            Assert.All(purchase.Items, x => Assert.Equal(100m, x.ReceivedQty));
            Assert.True(await db.AccountsPayable.AnyAsync(x => x.CompanyId == summary.CompanyId && x.PurchaseOrderId == purchase.Id));
            Assert.Equal(20, await db.StockMovements.CountAsync(x => x.CompanyId == summary.CompanyId && x.OriginType == "PurchaseOrder" && x.OriginId == purchase.Id));

            var clients = await db.Clients.AsNoTracking()
                .Where(x => x.CompanyId == summary.CompanyId && x.Email.EndsWith("@ninaerp.invalid"))
                .ToListAsync();
            Assert.Equal(4, clients.Count);
            Assert.Equal(4, clients.Select(x => x.State).Distinct().Count());
            Assert.Equal(new[] { "CE", "MA", "PE", "SP" }, clients.Select(x => x.State).OrderBy(x => x).ToArray());

            var sales = await db.Sales.AsNoTracking().Include(x => x.Items).Include(x => x.PaymentTransactions)
                .Where(x => x.CompanyId == summary.CompanyId && x.IdempotencyKey != null && x.IdempotencyKey.StartsWith("hml:sale:"))
                .ToListAsync();
            Assert.Equal(5, sales.Count);
            Assert.Equal(4, sales.Where(x => x.Status == SaleStatus.Completed).Select(x => x.ClientId).Distinct().Count());
            Assert.Equal(3, sales.Select(x => x.UserId).Distinct().Count());
            Assert.All(sales.Where(x => x.Status == SaleStatus.Completed), x => Assert.Contains(x.PaymentTransactions, p => p.Method == PaymentMethod.Cash && p.Status == PaymentTransactionStatus.Captured));

            var pending = Assert.Single(sales.Where(x => x.IdempotencyKey == "hml:sale:pix-pending"));
            Assert.Equal(SaleStatus.PaymentPending, pending.Status);
            var pendingPix = Assert.Single(pending.PaymentTransactions);
            Assert.Equal(PaymentMethod.Pix, pendingPix.Method);
            Assert.Equal(PaymentTransactionStatus.Pending, pendingPix.Status);
            Assert.Null(pendingPix.ExternalTransactionId);
            Assert.Null(pendingPix.CapturedAt);
        }
        finally
        {
            Environment.SetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable, old);
        }
    }

    [Fact]
    public async Task Homologation_Seed_Is_Idempotent()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var old = Environment.GetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable);
        Environment.SetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable, $"Hml!{Guid.NewGuid():N}Aa1");
        try
        {
            var first = await HomologationSeeder.SeedAsync(db);
            var countsBefore = await CountsAsync(db, first.CompanyId);
            var second = await HomologationSeeder.SeedAsync(db);
            var countsAfter = await CountsAsync(db, second.CompanyId);
            Assert.Equal(countsBefore, countsAfter);
        }
        finally
        {
            Environment.SetEnvironmentVariable(HomologationSeeder.PasswordEnvironmentVariable, old);
        }
    }

    [Fact]
    public async Task Tenant_Aware_Login_Repository_Does_Not_Cross_Company_Boundary()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var first = new Company { Cnpj = $"91{Random.Shared.NextInt64(100000000000, 999999999999)}", Name = "Tenant A", State = "CE", FiscalRegime = FiscalRegime.SimplesNacional };
        var second = new Company { Cnpj = $"92{Random.Shared.NextInt64(100000000000, 999999999999)}", Name = "Tenant B", State = "SP", FiscalRegime = FiscalRegime.SimplesNacional };
        db.AddRange(first, second);
        await db.SaveChangesAsync();
        const string username = "usuario-compartilhado";
        db.Users.AddRange(
            new User { CompanyId = first.Id, Username = username, FullName = "Usuário A", PasswordHash = "hash-a", Role = UserRole.Admin },
            new User { CompanyId = second.Id, Username = username, FullName = "Usuário B", PasswordHash = "hash-b", Role = UserRole.Admin });
        await db.SaveChangesAsync();

        var repository = new UserRepository(db);
        var userA = await repository.GetForLoginAsync(username, first.Cnpj);
        var userB = await repository.GetForLoginAsync(username, second.Cnpj);
        var wrong = await repository.GetForLoginAsync(username, "00.000.000/0000-00");
        Assert.Equal(first.Id, userA?.CompanyId);
        Assert.Equal(second.Id, userB?.CompanyId);
        Assert.Null(wrong);
    }

    [Fact]
    public async Task User_Administration_Protects_Last_Admin_And_Enforces_Strong_Passwords()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var company = new Company { Cnpj = $"93{Random.Shared.NextInt64(100000000000, 999999999999)}", Name = "RBAC Test", State = "CE", FiscalRegime = FiscalRegime.SimplesNacional };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var service = new UserAdministrationService(db);
        var adminId = await service.CreateAsync(company.Id, new("root", "Administrador Principal", "root@ninaerp.invalid", "Segura!2026-Root", UserRole.Admin), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessException>(() => service.ChangeRoleAsync(company.Id, adminId, adminId, UserRole.Manager, CancellationToken.None));
        await Assert.ThrowsAsync<BusinessException>(() => service.SetActiveAsync(company.Id, adminId, adminId, false, CancellationToken.None));
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(company.Id, new("weak", "Senha Fraca", "weak@ninaerp.invalid", "123456", UserRole.Cashier), CancellationToken.None));

        var secondAdmin = await service.CreateAsync(company.Id, new("root2", "Administrador Secundário", "root2@ninaerp.invalid", "Segura!2026-Root2", UserRole.Admin), CancellationToken.None);
        await service.ChangeRoleAsync(company.Id, secondAdmin, adminId, UserRole.Manager, CancellationToken.None);
        var changed = await db.Users.AsNoTracking().SingleAsync(x => x.Id == adminId);
        Assert.Equal(UserRole.Manager, changed.Role);
    }

    private static async Task<(int Users, int Products, int Clients, int Sales, int Transactions, int Movements)> CountsAsync(AppDbContext db, Guid companyId) =>
        (await db.Users.CountAsync(x => x.CompanyId == companyId),
         await db.Products.CountAsync(x => x.CompanyId == companyId && x.Sku.StartsWith("HML-")),
         await db.Clients.CountAsync(x => x.CompanyId == companyId && x.Email.EndsWith("@ninaerp.invalid")),
         await db.Sales.CountAsync(x => x.CompanyId == companyId && x.IdempotencyKey != null && x.IdempotencyKey.StartsWith("hml:sale:")),
         await db.PaymentTransactions.CountAsync(x => x.CompanyId == companyId),
         await db.StockMovements.CountAsync(x => x.CompanyId == companyId));
}
