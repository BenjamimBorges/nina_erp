using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
namespace NinaERP.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Cnpj == "00.000.000/0001-00");
        if (company is null)
        {
            company = new Company { Cnpj = "00.000.000/0001-00", Name = "Empresa Demo", FantasyName = "Nina Demo", State = "CE", City = "Fortaleza", FiscalRegime = FiscalRegime.SimplesNacional };
            db.Companies.Add(company); await db.SaveChangesAsync();
        }

        // Nunca cria senha conhecida no código-fonte. O seed de login só é habilitado
        // quando uma senha é fornecida explicitamente pelo ambiente de desenvolvimento.
        var demoPassword = Environment.GetEnvironmentVariable("NINAERP_DEMO_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(demoPassword) && !await db.Users.AnyAsync(x => x.CompanyId == company.Id && x.Username == "admin"))
        {
            db.Users.Add(new User { CompanyId = company.Id, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword), FullName = "Administrador Demo", Role = UserRole.Admin });
            await db.SaveChangesAsync();
        }

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.Code == "MATRIZ");
        if (warehouse is null) { warehouse = new Warehouse { CompanyId = company.Id, Code = "MATRIZ", Name = "Depósito Matriz", BranchCode = "MATRIZ", IsDefault = true }; db.Warehouses.Add(warehouse); }
        if (!await db.CashRegisters.AnyAsync(x => x.CompanyId == company.Id && x.Code == "CX01")) db.CashRegisters.Add(new CashRegister { CompanyId = company.Id, Code = "CX01", Name = "Caixa 01" });
        await db.SaveChangesAsync();

        if (!await db.Products.AnyAsync(x => x.CompanyId == company.Id))
        {
            db.Products.AddRange(
                new Product { CompanyId = company.Id, Sku = "PAP-A4-75", Name = "Papel A4 75g", Description = "Resma 500 folhas", Ncm = "48025610", Unit = "CX", PriceSale = 34.90m, PriceMinimum = 29.90m, CostAverage = 18.50m, StockQty = 120, StockMin = 30, Brand = "Demo", Department = "Papelaria", Barcode = "7891000001234" },
                new Product { CompanyId = company.Id, Sku = "DET-500", Name = "Detergente 500ml", Description = "Uso doméstico", Ncm = "34025000", Unit = "UN", PriceSale = 2.99m, PriceMinimum = 2.50m, CostAverage = 1.98m, StockQty = 20, StockMin = 20, Brand = "Demo", Department = "Limpeza", Barcode = "7894900000567" }
            );
            await db.SaveChangesAsync();
        }
        var products = await db.Products.Where(x => x.CompanyId == company.Id && x.IsActive).ToListAsync();
        foreach (var p in products)
        {
            if (!await db.InventoryBalances.AnyAsync(x => x.CompanyId == company.Id && x.WarehouseId == warehouse.Id && x.ProductId == p.Id))
            {
                db.InventoryBalances.Add(new InventoryBalance { CompanyId = company.Id, WarehouseId = warehouse.Id, ProductId = p.Id, OnHand = p.StockQty, AverageCost = p.CostAverage });
                if (p.StockQty > 0 && !await db.StockMovements.AnyAsync(x => x.CompanyId == company.Id && x.ProductId == p.Id))
                    db.StockMovements.Add(new StockMovement { CompanyId = company.Id, ProductId = p.Id, WarehouseId = warehouse.Id, Type = StockMovementType.Entry, Qty = p.StockQty, CostUnit = p.CostAverage, OriginType = "DemoSeed", IdempotencyKey = $"demo-seed:{p.Id:N}", Notes = "Saldo inicial de demonstração" });
            }
        }
        await db.SaveChangesAsync();
        // Intencionalmente NÃO cria NF-e/NFC-e autorizada, protocolo, chave de acesso,
        // pagamento externo confirmado ou qualquer evidência fiscal/bancária fictícia.
    }
}
