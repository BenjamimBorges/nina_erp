using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;
using NinaERP.Infrastructure.Services;
using Xunit;

namespace NinaERP.Tests;

[Trait("Category", "Integration")]
[Collection("Postgres")]
public sealed class ExpansionIntegrationTests
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("NINAERP_TEST_CONNECTION")
        ?? throw new InvalidOperationException("NINAERP_TEST_CONNECTION é obrigatório para testes de integração PostgreSQL.");

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Approval_Requires_Four_Eyes_And_Correct_Role()
    {
        await using var db = CreateDb(); await db.Database.MigrateAsync();
        var company = NewCompany("41");
        var requester = NewUser(company, UserRole.Cashier, "req");
        var manager = NewUser(company, UserRole.Manager, "mgr");
        var register = new CashRegister { CompanyId = company.Id, Code = $"APR-{Guid.NewGuid():N}"[..20], Name = "Caixa aprovação" };
        db.AddRange(company, requester, manager, register); await db.SaveChangesAsync();
        var session = new CashSession { CompanyId = company.Id, CashRegisterId = register.Id, UserId = requester.Id, OpeningAmount = 100m, ExpectedAmount = 100m };
        db.CashSessions.Add(session); await db.SaveChangesAsync();
        var service = new ApprovalService(db);
        var resourceId = session.Id;
        var approval = await service.RequestAsync(company.Id, requester.Id, ApprovalType.CashMovement,
            "CashSession", resourceId, UserRole.Manager, "Sangria excepcional de homologação",
            null, $"approval-{Guid.NewGuid():N}", DateTime.UtcNow.AddMinutes(30), CancellationToken.None);

        await Assert.ThrowsAsync<BusinessException>(() => service.DecideAsync(company.Id, requester.Id, approval.Id, true, null, CancellationToken.None));
        var decided = await service.DecideAsync(company.Id, manager.Id, approval.Id, true, "Conferido pelo gerente", CancellationToken.None);
        Assert.Equal(ApprovalStatus.Approved, decided.Status);
        Assert.Equal(manager.Id, decided.DecidedByUserId);
        Assert.True(await service.IsApprovedAsync(company.Id, approval.Id, ApprovalType.CashMovement, resourceId, CancellationToken.None));
    }

    [Fact]
    public async Task Omnichannel_Is_Idempotent_And_Rejects_Divergent_Replay()
    {
        await using var db = CreateDb(); await db.Database.MigrateAsync();
        var company = NewCompany("42"); var user = NewUser(company, UserRole.Manager, "omni");
        var product = new Product { CompanyId = company.Id, Sku = $"SKU-{Guid.NewGuid():N}"[..18], Barcode = "7891234567895", Name = "Produto Omnichannel", PriceSale = 19.90m, PriceMinimum = 15m, CostAverage = 10m, StockMin = 2m };
        db.AddRange(company, user, product); await db.SaveChangesAsync();
        var service = new OmnichannelService(db);
        var key = $"omni-{Guid.NewGuid():N}"; var external = $"EXT-{Guid.NewGuid():N}";
        var input = new ReceiveChannelOrderInput(SalesChannel.Api, external, "customer-1", null, "BRL", 39.80m, key,
            [new ChannelOrderItemInput(product.Sku, product.Barcode, 2m, 19.90m)]);
        var first = await service.ReceiveAsync(company.Id, user.Id, input, CancellationToken.None);
        var replay = await service.ReceiveAsync(company.Id, user.Id, input, CancellationToken.None);
        Assert.Equal(first.Id, replay.Id); Assert.Equal(ChannelOrderStatus.Validated, first.Status);

        var divergent = input with { Total = 38.80m, Items = [new ChannelOrderItemInput(product.Sku, product.Barcode, 2m, 19.40m)] };
        await Assert.ThrowsAsync<BusinessException>(() => service.ReceiveAsync(company.Id, user.Id, divergent, CancellationToken.None));
    }

    [Fact]
    public async Task Assistant_Blocks_Sensitive_Action_And_Audits_Without_Executing_It()
    {
        await using var db = CreateDb(); await db.Database.MigrateAsync();
        var company = NewCompany("43"); var user = NewUser(company, UserRole.Admin, "assist");
        db.AddRange(company, user); await db.SaveChangesAsync();
        var service = new AssistantService(new AdvancedBiService(db), new MobileService(db), db);
        const string question = "emitir nota e estornar pagamento da venda";
        var answer = await service.AskAsync(company.Id, user.Id, question, CancellationToken.None);
        Assert.True(answer.Blocked);
        var audit = await db.AssistantInteractions.AsNoTracking().SingleAsync(x => x.CompanyId == company.Id && x.UserId == user.Id);
        Assert.NotEqual(question, audit.QuestionHash); Assert.False(audit.ResponseSummary.Contains(question, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(64, audit.QuestionHash.Length);
    }

    [Fact]
    public async Task Distributed_Lease_Is_Exclusive_And_Can_Be_Reacquired_After_Expiry()
    {
        await using var db1 = CreateDb(); await db1.Database.MigrateAsync();
        await using var db2 = CreateDb();
        var first = new ClusterCoordinationService(db1); var second = new ClusterCoordinationService(db2);
        var lease = $"test-{Guid.NewGuid():N}";
        Assert.True(await first.TryAcquireLeaseAsync(lease, "node-a", TimeSpan.FromMinutes(1), CancellationToken.None));
        Assert.False(await second.TryAcquireLeaseAsync(lease, "node-b", TimeSpan.FromMinutes(1), CancellationToken.None));
        var persisted = await db1.DistributedLeases.SingleAsync(x => x.Name == lease);
        persisted.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await db1.SaveChangesAsync();
        db1.ChangeTracker.Clear();
        Assert.True(await second.TryAcquireLeaseAsync(lease, "node-b", TimeSpan.FromMinutes(1), CancellationToken.None));
    }

    [Fact]
    public async Task Mobile_Low_Stock_Never_Leaks_Another_Company()
    {
        await using var db = CreateDb(); await db.Database.MigrateAsync();
        var a = NewCompany("46"); var b = NewCompany("47");
        var wa = new Warehouse { CompanyId = a.Id, Code = $"WA-{Guid.NewGuid():N}"[..20], Name = "Depósito A", IsDefault = true };
        var wb = new Warehouse { CompanyId = b.Id, Code = $"WB-{Guid.NewGuid():N}"[..20], Name = "Depósito B", IsDefault = true };
        var pa = new Product { CompanyId = a.Id, Sku = $"A-{Guid.NewGuid():N}"[..18], Barcode = "7891000000016", Name = "Produto A", PriceSale = 10m, CostAverage = 5m, StockMin = 5m };
        var pb = new Product { CompanyId = b.Id, Sku = $"B-{Guid.NewGuid():N}"[..18], Barcode = "7891000000023", Name = "SEGREDO TENANT B", PriceSale = 10m, CostAverage = 5m, StockMin = 5m };
        db.AddRange(a,b,wa,wb,pa,pb); await db.SaveChangesAsync();
        db.InventoryBalances.AddRange(
            new InventoryBalance { CompanyId = a.Id, WarehouseId = wa.Id, ProductId = pa.Id, OnHand = 1m, AverageCost = 5m },
            new InventoryBalance { CompanyId = b.Id, WarehouseId = wb.Id, ProductId = pb.Id, OnHand = 0m, AverageCost = 5m });
        await db.SaveChangesAsync();
        var rows = await new MobileService(db).GetLowStockAsync(a.Id, 20, CancellationToken.None);
        Assert.Contains(rows, x => x.ProductId == pa.Id);
        Assert.False(rows.Any(x => x.ProductId == pb.Id || x.Name.Contains("TENANT B", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Advanced_BI_Materialized_View_Remains_Company_Scoped()
    {
        await using var db = CreateDb(); await db.Database.MigrateAsync();
        var a = NewCompany("44"); var b = NewCompany("45"); db.AddRange(a,b); await db.SaveChangesAsync();
        var bi = new AdvancedBiService(db); await bi.RefreshAsync(CancellationToken.None);
        var rowsA = await bi.GetMonthlyAsync(a.Id, 3, CancellationToken.None);
        var rowsB = await bi.GetMonthlyAsync(b.Id, 3, CancellationToken.None);
        Assert.Equal(3, rowsA.Count); Assert.Equal(3, rowsB.Count);
        Assert.All(rowsA, x => Assert.Equal(0m, x.NetSales)); Assert.All(rowsB, x => Assert.Equal(0m, x.NetSales));
    }

    private static Company NewCompany(string prefix) => new()
    {
        Cnpj = prefix + Random.Shared.NextInt64(100000000000, 999999999999), Name = $"Empresa {prefix}",
        State = "CE", FiscalRegime = FiscalRegime.SimplesNacional, TimeZoneId = "America/Fortaleza"
    };

    private static User NewUser(Company company, UserRole role, string prefix) => new()
    {
        CompanyId = company.Id, Username = $"{prefix}-{Guid.NewGuid():N}"[..24], PasswordHash = "not-used-in-service-test",
        FullName = $"Usuário {prefix}", Email = $"{prefix}-{Guid.NewGuid():N}@ninaerp.invalid", Role = role
    };
}
