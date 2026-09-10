using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Sales.Commands;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Services;

namespace NinaERP.Infrastructure.Persistence;

/// <summary>
/// Massa determinística de HOMOLOGAÇÃO. Nunca deve ser executada em produção.
/// Senhas são recebidas exclusivamente por variável de ambiente e não são persistidas em texto claro.
/// </summary>
public static class HomologationSeeder
{
    public const string CompanyCnpj = "12.345.678/0001-95";
    public const string PasswordEnvironmentVariable = "NINAERP_HOMOLOGATION_PASSWORD";

    public static readonly string[] AdministrativeUsers = ["admin_hml", "gerente_hml", "estoque_hml"];
    public static readonly string[] Sellers = ["vendedor01", "vendedor02", "vendedor03"];

    public static async Task<HomologationSeedSummary> SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"{PasswordEnvironmentVariable} é obrigatória para gerar a massa de homologação.");
        UserAdministrationService.ValidatePassword(password);

        var company = await db.Companies.FirstOrDefaultAsync(x => x.Cnpj == CompanyCnpj, ct);
        if (company is null)
        {
            company = new Company
            {
                Cnpj = CompanyCnpj,
                Name = "Nina Homologação Comércio Ltda",
                FantasyName = "Nina HML",
                StateRegistration = "110042490114",
                Address = "Avenida de Homologação, 1000",
                City = "Fortaleza",
                State = "CE",
                ZipCode = "60000-000",
                Email = "homologacao@ninaerp.invalid",
                TimeZoneId = "America/Fortaleza",
                FiscalRegime = FiscalRegime.SimplesNacional
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync(ct);
        }

        await EnsureUsersAsync(db, company.Id, password, ct);
        var warehouse = await EnsureWarehouseAsync(db, company.Id, ct);
        var registers = await EnsureRegistersAsync(db, company.Id, ct);
        var products = await EnsureProductsAsync(db, company.Id, warehouse.Id, ct);
        var clients = await EnsureClientsAsync(db, company.Id, ct);
        var supplier = await EnsureSupplierAsync(db, company.Id, ct);

        var sellerUsers = await db.Users.Where(x => x.CompanyId == company.Id && Sellers.Contains(x.Username)).OrderBy(x => x.Username).ToListAsync(ct);
        var adminUserId = await db.Users.Where(x => x.CompanyId == company.Id && x.Username == "admin_hml").Select(x => x.Id).SingleAsync(ct);
        await EnsurePilotPurchaseAsync(db, company.Id, adminUserId, supplier.Id, warehouse.Id, products, ct);
        var cash = new CashManagementService(db);
        var payment = new PaymentService(db);
        var uow = new UnitOfWork(db);
        var createSale = new CreateSaleHandler(uow, cash);

        var sessions = new Dictionary<Guid, CashSessionDto>();
        for (var i = 0; i < sellerUsers.Count; i++)
        {
            var seller = sellerUsers[i];
            var current = await cash.GetOpenSessionAsync(company.Id, seller.Id, ct);
            sessions[seller.Id] = current ?? await cash.OpenSessionAsync(
                company.Id,
                seller.Id,
                registers[i].Id,
                200m,
                $"hml:cash-open:{seller.Username}",
                ct);
        }

        var scenarios = new[]
        {
            new SaleScenario("hml:sale:cliente-ce", clients[0].Id, sellerUsers[0].Id, new[] { (products[0].Id, 2m), (products[1].Id, 3m) }),
            new SaleScenario("hml:sale:cliente-sp", clients[1].Id, sellerUsers[1].Id, new[] { (products[2].Id, 1m), (products[3].Id, 4m), (products[4].Id, 2m) }),
            new SaleScenario("hml:sale:cliente-pe", clients[2].Id, sellerUsers[2].Id, new[] { (products[5].Id, 5m), (products[6].Id, 2m) }),
            new SaleScenario("hml:sale:cliente-ma", clients[3].Id, sellerUsers[0].Id, new[] { (products[7].Id, 2m), (products[8].Id, 1m), (products[9].Id, 3m), (products[10].Id, 2m) })
        };

        foreach (var scenario in scenarios)
        {
            if (await db.Sales.AnyAsync(x => x.CompanyId == company.Id && x.IdempotencyKey == scenario.Key, ct))
                continue;

            var session = sessions[scenario.SellerId];
            var saleId = await createSale.Handle(new CreateSaleCommand(
                company.Id,
                scenario.SellerId,
                session.Id,
                warehouse.Id,
                scenario.Key,
                scenario.ClientId,
                scenario.Items.Select(x => new CreateSaleItem(x.ProductId, x.Qty)).ToArray()), ct);

            var saleTotal = await db.Sales.Where(x => x.Id == saleId).Select(x => x.TotalProducts - x.TotalDiscount).SingleAsync(ct);
            await payment.RegisterAsync(
                company.Id,
                scenario.SellerId,
                saleId,
                session.Id,
                [new PaymentRequestDto(PaymentMethod.Cash, saleTotal, $"{scenario.Key}:cash")],
                ct);
        }

        var pendingKey = "hml:sale:pix-pending";
        if (!await db.Sales.AnyAsync(x => x.CompanyId == company.Id && x.IdempotencyKey == pendingKey, ct))
        {
            var seller = sellerUsers[1];
            var session = sessions[seller.Id];
            var saleId = await createSale.Handle(new CreateSaleCommand(
                company.Id, seller.Id, session.Id, warehouse.Id, pendingKey, clients[1].Id,
                [new CreateSaleItem(products[11].Id, 1m), new CreateSaleItem(products[12].Id, 2m)], ct);
            var total = await db.Sales.Where(x => x.Id == saleId).Select(x => x.TotalProducts - x.TotalDiscount).SingleAsync(ct);
            await payment.RegisterAsync(company.Id, seller.Id, saleId, session.Id,
                [new PaymentRequestDto(PaymentMethod.Pix, total, $"{pendingKey}:pix", "homologation-not-configured")], ct);
        }

        return new HomologationSeedSummary(
            company.Id,
            await db.Users.CountAsync(x => x.CompanyId == company.Id && x.IsActive, ct),
            await db.Products.CountAsync(x => x.CompanyId == company.Id && x.IsActive, ct),
            await db.Clients.CountAsync(x => x.CompanyId == company.Id && x.IsActive, ct),
            sellerUsers.Count,
            await db.Sales.CountAsync(x => x.CompanyId == company.Id && x.IsActive, ct));
    }

    private static async Task EnsureUsersAsync(AppDbContext db, Guid companyId, string password, CancellationToken ct)
    {
        var definitions = new[]
        {
            new UserSeed("admin_hml", "Administrador Homologação", "admin.hml@ninaerp.invalid", UserRole.Admin),
            new UserSeed("gerente_hml", "Gerente Homologação", "gerente.hml@ninaerp.invalid", UserRole.Manager),
            new UserSeed("estoque_hml", "Supervisor de Estoque HML", "estoque.hml@ninaerp.invalid", UserRole.Stock),
            new UserSeed("vendedor01", "Vendedor Um", "vendedor01@ninaerp.invalid", UserRole.Cashier),
            new UserSeed("vendedor02", "Vendedor Dois", "vendedor02@ninaerp.invalid", UserRole.Cashier),
            new UserSeed("vendedor03", "Vendedor Três", "vendedor03@ninaerp.invalid", UserRole.Cashier)
        };

        foreach (var definition in definitions)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Username == definition.Username, ct);
            if (user is null)
            {
                db.Users.Add(new User
                {
                    CompanyId = companyId,
                    Username = definition.Username,
                    FullName = definition.FullName,
                    Email = definition.Email,
                    Role = definition.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)
                });
            }
            else
            {
                user.FullName = definition.FullName;
                user.Email = definition.Email;
                user.Role = definition.Role;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow; // também revoga sessões antigas da massa ao re-seed
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Warehouse> EnsureWarehouseAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == "HML-MATRIZ", ct);
        if (warehouse is null)
        {
            warehouse = new Warehouse { CompanyId = companyId, Code = "HML-MATRIZ", Name = "Depósito Homologação", BranchCode = "MATRIZ", IsDefault = true };
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(ct);
        }
        return warehouse;
    }

    private static async Task<IReadOnlyList<CashRegister>> EnsureRegistersAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        var result = new List<CashRegister>();
        for (var i = 1; i <= 3; i++)
        {
            var code = $"HML-CX{i:00}";
            var register = await db.CashRegisters.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code, ct);
            if (register is null)
            {
                register = new CashRegister { CompanyId = companyId, Code = code, Name = $"Caixa Homologação {i:00}" };
                db.CashRegisters.Add(register);
                await db.SaveChangesAsync(ct);
            }
            result.Add(register);
        }
        return result;
    }

    private static async Task<IReadOnlyList<Product>> EnsureProductsAsync(AppDbContext db, Guid companyId, Guid warehouseId, CancellationToken ct)
    {
        var seeds = ProductSeeds();
        foreach (var seed in seeds)
        {
            var product = await db.Products.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Sku == seed.Sku, ct);
            if (product is null)
            {
                product = new Product
                {
                    CompanyId = companyId,
                    Sku = seed.Sku,
                    Barcode = seed.Barcode,
                    Name = seed.Name,
                    Description = "Produto de homologação",
                    Brand = seed.Brand,
                    Department = seed.Department,
                    Unit = "UN",
                    Ncm = seed.Ncm,
                    PriceSale = seed.SalePrice,
                    PriceMinimum = decimal.Round(seed.SalePrice * 0.90m, 2),
                    CostAverage = seed.PurchasePrice,
                    StockQty = 0m,
                    StockMin = 10m
                };
                db.Products.Add(product);
                await db.SaveChangesAsync(ct);
            }

            // O saldo é criado pelo recebimento do pedido HML-PO-001. O seed não injeta estoque diretamente.
            // Isso permite que a homologação percorra compra -> estoque -> financeiro de forma real.
            if (product.StockQty < 0)
                throw new InvalidOperationException($"Produto de homologação {product.Sku} possui estoque agregado inválido.");
        }
        return await db.Products.Where(x => x.CompanyId == companyId && x.Sku.StartsWith("HML-")).OrderBy(x => x.Sku).ToListAsync(ct);
    }

    private static async Task<IReadOnlyList<Client>> EnsureClientsAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        var definitions = new[]
        {
            new ClientSeed("123.456.789-09", "Cliente Ceará", "cliente.ce@ninaerp.invalid", "Fortaleza", "CE", "60000-100"),
            new ClientSeed("987.654.321-00", "Cliente São Paulo", "cliente.sp@ninaerp.invalid", "São Paulo", "SP", "01000-100"),
            new ClientSeed("111.222.333-96", "Cliente Pernambuco", "cliente.pe@ninaerp.invalid", "Recife", "PE", "50000-100"),
            new ClientSeed("529.982.247-25", "Cliente Maranhão", "cliente.ma@ninaerp.invalid", "São Luís", "MA", "65000-100")
        };
        foreach (var definition in definitions)
        {
            if (await db.Clients.AnyAsync(x => x.CompanyId == companyId && x.Document == definition.Document, ct)) continue;
            db.Clients.Add(new Client
            {
                CompanyId = companyId,
                Document = definition.Document,
                Name = definition.Name,
                FantasyName = definition.Name,
                Email = definition.Email,
                Phone = "(85) 90000-0000",
                Address = "Rua de Homologação, 100",
                City = definition.City,
                State = definition.State,
                ZipCode = definition.Zip,
                CreditLimit = 5000m
            });
        }
        await db.SaveChangesAsync(ct);
        var clients = await db.Clients.Where(x => x.CompanyId == companyId && x.Email.EndsWith("@ninaerp.invalid")).ToListAsync(ct);
        return definitions.Select(d => clients.Single(x => x.Document == d.Document)).ToList();
    }

    private static async Task<Supplier> EnsureSupplierAsync(AppDbContext db, Guid companyId, CancellationToken ct)
    {
        const string document = "45.987.654/0001-10";
        var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Document == document, ct);
        if (supplier is not null) return supplier;
        supplier = new Supplier
        {
            CompanyId = companyId, Document = document, Name = "Fornecedor Homologação Ltda", FantasyName = "Fornecedor HML",
            Email = "fornecedor@ninaerp.invalid", Phone = "(85) 3333-0000", Address = "Av. de Homologação, 500", City = "Fortaleza",
            State = "CE", ZipCode = "60000-500", ContactName = "Equipe HML"
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier;
    }

    private static async Task EnsurePilotPurchaseAsync(AppDbContext db, Guid companyId, Guid userId, Guid supplierId, Guid warehouseId, IReadOnlyList<Product> products, CancellationToken ct)
    {
        const string number = "HML-PO-001";
        var purchase = new PurchaseService(db);
        var existing = await db.PurchaseOrders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Number == number)
            .Select(x => new { x.Id, x.Status })
            .FirstOrDefaultAsync(ct);

        Guid orderId;
        PurchaseOrderStatus status;
        if (existing is null)
        {
            var seeds = ProductSeeds().ToDictionary(x => x.Sku, StringComparer.OrdinalIgnoreCase);
            var created = await purchase.CreateAsync(
                companyId, userId, supplierId, warehouseId, number,
                products.Select(p => new PurchaseItemInput(p.Id, 100m, seeds[p.Sku].PurchasePrice)).ToArray(),
                DateTime.UtcNow, "Pedido controlado para homologação funcional", ct);
            orderId = created.Id;
            status = created.Status;
        }
        else
        {
            orderId = existing.Id;
            status = existing.Status;
        }

        if (status != PurchaseOrderStatus.Received)
            await purchase.ReceiveAsync(companyId, userId, orderId, "hml:purchase:HML-PO-001:receive", null, DateTime.UtcNow.AddDays(30), ct);
    }

    private static ProductSeed[] ProductSeeds() =>
    [
        new("HML-001", "7899000000010", "Arroz Tipo 1 5kg", "Alimentos", "Nina HML", "10063021", 18.40m, 27.90m),
        new("HML-002", "7899000000027", "Feijão Carioca 1kg", "Alimentos", "Nina HML", "07133319", 5.90m, 9.49m),
        new("HML-003", "7899000000034", "Café Torrado 500g", "Mercearia", "Nina HML", "09012100", 12.30m, 18.90m),
        new("HML-004", "7899000000041", "Açúcar Cristal 1kg", "Mercearia", "Nina HML", "17019900", 3.40m, 5.59m),
        new("HML-005", "7899000000058", "Óleo de Soja 900ml", "Mercearia", "Nina HML", "15079011", 5.80m, 8.99m),
        new("HML-006", "7899000000065", "Leite Integral 1L", "Laticínios", "Nina HML", "04012010", 3.25m, 5.49m),
        new("HML-007", "7899000000072", "Biscoito Cream Cracker", "Mercearia", "Nina HML", "19053100", 2.70m, 4.79m),
        new("HML-008", "7899000000089", "Detergente Neutro 500ml", "Limpeza", "Nina HML", "34025000", 1.65m, 2.99m),
        new("HML-009", "7899000000096", "Sabão em Pó 1kg", "Limpeza", "Nina HML", "34025000", 6.10m, 9.89m),
        new("HML-010", "7899000000102", "Desinfetante 1L", "Limpeza", "Nina HML", "38089419", 3.20m, 5.69m),
        new("HML-011", "7899000000119", "Papel Higiênico 4un", "Higiene", "Nina HML", "48181000", 5.10m, 8.49m),
        new("HML-012", "7899000000126", "Creme Dental 90g", "Higiene", "Nina HML", "33061000", 3.35m, 5.99m),
        new("HML-013", "7899000000133", "Sabonete 85g", "Higiene", "Nina HML", "34011190", 1.45m, 2.79m),
        new("HML-014", "7899000000140", "Água Mineral 500ml", "Bebidas", "Nina HML", "22011000", 0.95m, 2.00m),
        new("HML-015", "7899000000157", "Refrigerante Cola 2L", "Bebidas", "Nina HML", "22021000", 5.85m, 9.49m),
        new("HML-016", "7899000000164", "Suco de Uva 1L", "Bebidas", "Nina HML", "20096900", 6.40m, 10.90m),
        new("HML-017", "7899000000171", "Macarrão Espaguete 500g", "Alimentos", "Nina HML", "19021900", 2.45m, 4.39m),
        new("HML-018", "7899000000188", "Molho de Tomate 300g", "Mercearia", "Nina HML", "21032010", 1.55m, 2.89m),
        new("HML-019", "7899000000195", "Farinha de Trigo 1kg", "Alimentos", "Nina HML", "11010010", 3.65m, 6.29m),
        new("HML-020", "7899000000201", "Sal Refinado 1kg", "Mercearia", "Nina HML", "25010020", 1.20m, 2.39m)
    ];

    private sealed record UserSeed(string Username, string FullName, string Email, UserRole Role);
    private sealed record ProductSeed(string Sku, string Barcode, string Name, string Department, string Brand, string Ncm, decimal PurchasePrice, decimal SalePrice);
    private sealed record ClientSeed(string Document, string Name, string Email, string City, string State, string Zip);
    private sealed record SaleScenario(string Key, Guid ClientId, Guid SellerId, IReadOnlyList<(Guid ProductId, decimal Qty)> Items);
}

public sealed record HomologationSeedSummary(Guid CompanyId, int Users, int Products, int Clients, int Sellers, int Sales);
