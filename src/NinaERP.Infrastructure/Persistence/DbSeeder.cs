using Microsoft.EntityFrameworkCore;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;

namespace NinaERP.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Cnpj == "00.000.000/0001-00");
        if (company == null)
        {
            company = new Company
            {
                Cnpj = "00.000.000/0001-00",
                Name = "Empresa Demo",
                FantasyName = "Demo ERP",
                State = "MT",
                City = "Cuiabá",
                FiscalRegime = FiscalRegime.SimplesNacional
            };
            db.Companies.Add(company);
            await db.SaveChangesAsync();
        }

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin == null)
        {
            admin = new User
            {
                CompanyId = company.Id,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Administrador",
                Role = UserRole.Admin
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        var vendedor = await db.Users.FirstOrDefaultAsync(u => u.Username == "vendedor");
        if (vendedor == null)
        {
            vendedor = new User
            {
                CompanyId = company.Id,
                Username = "vendedor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendedor@123"),
                FullName = "João Vendedor",
                Role = UserRole.Manager
            };
            db.Users.Add(vendedor);
            await db.SaveChangesAsync();
        }

        if (!await db.Clients.AnyAsync(c => c.CompanyId == company.Id))
        {
            db.Clients.AddRange(
                new Client
                {
                    CompanyId = company.Id,
                    Document = "123.456.789-09",
                    IsLegalEntity = false,
                    Name = "João da Silva",
                    FantasyName = "João Varejo",
                    Email = "joao.silva@demoerp.com",
                    Phone = "(65) 99999-0001",
                    Address = "Av. Brasil, 1020",
                    City = "Cuiabá",
                    State = "MT",
                    ZipCode = "78000-000",
                    CreditLimit = 5000m
                },
                new Client
                {
                    CompanyId = company.Id,
                    Document = "987.654.321-00",
                    IsLegalEntity = false,
                    Name = "Maria Souza",
                    FantasyName = "Loja Maria",
                    Email = "maria.souza@demoerp.com",
                    Phone = "(65) 99999-0002",
                    Address = "Rua 13 de Junho, 451",
                    City = "Cuiabá",
                    State = "MT",
                    ZipCode = "78005-100",
                    CreditLimit = 3000m
                },
                new Client
                {
                    CompanyId = company.Id,
                    Document = "01.234.567/0001-89",
                    IsLegalEntity = true,
                    Name = "Distribuidora Central",
                    FantasyName = "Districentral",
                    Email = "contato@districentral.com",
                    Phone = "(65) 3333-4444",
                    Address = "Av. Brasil, 2100",
                    City = "Cuiabá",
                    State = "MT",
                    ZipCode = "78020-200",
                    CreditLimit = 15000m
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Suppliers.AnyAsync(s => s.CompanyId == company.Id))
        {
            db.Suppliers.AddRange(
                new Supplier
                {
                    CompanyId = company.Id,
                    Document = "12.345.678/0001-90",
                    IsLegalEntity = true,
                    Name = "Fornecedor Tecno",
                    FantasyName = "Tecno Supplies",
                    StateRegistration = "123456789",
                    Email = "compras@tecnosupplies.com",
                    Phone = "(65) 3333-2222",
                    Address = "Rua das Indústrias, 500",
                    City = "Cuiabá",
                    State = "MT",
                    ZipCode = "78010-300",
                    ContactName = "Carlos Ramos"
                },
                new Supplier
                {
                    CompanyId = company.Id,
                    Document = "98.765.432/0001-10",
                    IsLegalEntity = true,
                    Name = "Alimentos Bom Sabor",
                    FantasyName = "Bom Sabor",
                    StateRegistration = "987654321",
                    Email = "contato@bomsabor.com",
                    Phone = "(65) 3333-1111",
                    Address = "Av. do Comércio, 780",
                    City = "Cuiabá",
                    State = "MT",
                    ZipCode = "78030-400",
                    ContactName = "Renata Lima"
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync(p => p.CompanyId == company.Id))
        {
            var products = new[]
            {
                new Product
                {
                    CompanyId = company.Id,
                    Sku = "NINA-001",
                    Name = "Camiseta Básica",
                    Description = "Camiseta 100% algodão branca",
                    Ncm = "61091000",
                    Cest = "0101010",
                    Unit = "UN",
                    PriceSale = 100.00m,
                    PriceMinimum = 65.00m,
                    CostAverage = 30.00m,
                    StockQty = 500,
                    StockMin = 50,
                    Brand = "Nina",
                    Department = "Vestuário",
                    Barcode = "7891234567890"
                },
                new Product
                {
                    CompanyId = company.Id,
                    Sku = "NINA-002",
                    Name = "Calça Jeans",
                    Description = "Calça jeans escura masculina",
                    Ncm = "62034200",
                    Cest = "0202020",
                    Unit = "UN",
                    PriceSale = 129.90m,
                    PriceMinimum = 99.90m,
                    CostAverage = 60.00m,
                    StockQty = 45,
                    StockMin = 15,
                    Brand = "Nina",
                    Department = "Vestuário",
                    Barcode = "7891234567891"
                },
                new Product
                {
                    CompanyId = company.Id,
                    Sku = "NINA-003",
                    Name = "Caderno A4 100 folhas",
                    Description = "Caderno universitário capa dura",
                    Ncm = "48201000",
                    Cest = "0303030",
                    Unit = "UN",
                    PriceSale = 12.50m,
                    PriceMinimum = 9.90m,
                    CostAverage = 5.00m,
                    StockQty = 260,
                    StockMin = 40,
                    Brand = "Nina",
                    Department = "Papelaria",
                    Barcode = "7891234567892"
                },
                new Product
                {
                    CompanyId = company.Id,
                    Sku = "NINA-004",
                    Name = "Caneta Esferográfica Azul",
                    Description = "Caneta esferográfica 0.7mm",
                    Ncm = "96081000",
                    Cest = "0404040",
                    Unit = "UN",
                    PriceSale = 2.90m,
                    PriceMinimum = 1.90m,
                    CostAverage = 0.90m,
                    StockQty = 520,
                    StockMin = 200,
                    Brand = "Nina",
                    Department = "Papelaria",
                    Barcode = "7891234567893"
                },
                new Product
                {
                    CompanyId = company.Id,
                    Sku = "NINA-005",
                    Name = "Notebook Dell",
                    Description = "Notebook Dell com processador Intel Core i7",
                    Ncm = "84713012",
                    Cest = "0505050",
                    Unit = "UN",
                    PriceSale = 5000.00m,
                    PriceMinimum = 3250.00m,
                    CostAverage = 2500.00m,
                    StockQty = 150,
                    StockMin = 5,
                    Brand = "Dell",
                    Department = "Informática",
                    Barcode = "7891234567894"
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            if (!await db.StockMovements.AnyAsync(sm => sm.CompanyId == company.Id))
            {
                db.StockMovements.AddRange(
                    new StockMovement
                    {
                        CompanyId = company.Id,
                        ProductId = products[0].Id,
                        Type = StockMovementType.Entry,
                        Qty = 500,
                        CostUnit = 30.00m,
                        OriginType = "Compra inicial",
                        Notes = "Entrada inicial de camisetas em estoque"
                    },
                    new StockMovement
                    {
                        CompanyId = company.Id,
                        ProductId = products[4].Id,
                        Type = StockMovementType.Entry,
                        Qty = 150,
                        CostUnit = 2500.00m,
                        OriginType = "Compra inicial",
                        Notes = "Entrada inicial de notebooks em estoque"
                    },
                    new StockMovement
                    {
                        CompanyId = company.Id,
                        ProductId = products[1].Id,
                        Type = StockMovementType.Loss,
                        Qty = 2,
                        CostUnit = 60.00m,
                        OriginType = "Perda",
                        Notes = "Produto danificado"
                    }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Sales.AnyAsync(s => s.CompanyId == company.Id))
            {
                // Obter clientes para varejo e atacado
                var clienteVarejo = await db.Clients.FirstOrDefaultAsync(c => c.CompanyId == company.Id && c.Document == "123.456.789-09");
                var clienteAtacado = await db.Clients.FirstOrDefaultAsync(c => c.CompanyId == company.Id && c.Document == "01.234.567/0001-89");

                if (clienteVarejo == null || clienteAtacado == null)
                    return;

                // Preços de varejo
                decimal camisetaVarejo = 100.00m;
                decimal notebookVarejo = 5000.00m;

                // Preços de atacado (35% desconto = 65% do valor)
                decimal camisetaAtacado = camisetaVarejo * 0.65m;
                decimal notebookAtacado = notebookVarejo * 0.65m;

                // ===== VENDA VAREJO (NFC-e) =====
                var saleVarejo = new Sale
                {
                    CompanyId = company.Id,
                    ClientId = clienteVarejo.Id,
                    UserId = vendedor.Id,
                    TotalProducts = (2 * camisetaVarejo) + notebookVarejo,
                    TotalDiscount = 0m,
                    TotalPaid = (2 * camisetaVarejo) + notebookVarejo,
                    Status = SaleStatus.Completed,
                    FiscalModel = InvoiceModel.NFCeModel65,
                    SaleDate = DateTime.UtcNow
                };

                saleVarejo.Items.Add(new SaleItem
                {
                    ProductId = products[0].Id, // Camiseta Básica
                    Qty = 2,
                    UnitPrice = camisetaVarejo,
                    Discount = 0m,
                    Cfop = "5102",
                    IcmsValue = 0m,
                    PisValue = 0m,
                    CofinsValue = 0m
                });

                saleVarejo.Items.Add(new SaleItem
                {
                    ProductId = products[4].Id, // Notebook Dell
                    Qty = 1,
                    UnitPrice = notebookVarejo,
                    Discount = 0m,
                    Cfop = "5102",
                    IcmsValue = 0m,
                    PisValue = 0m,
                    CofinsValue = 0m
                });

                saleVarejo.Payments.Add(new Payment
                {
                    Method = PaymentMethod.Cash,
                    Amount = saleVarejo.TotalPaid,
                    PaidAt = DateTime.UtcNow
                });

                var invoiceVarejo = new Invoice
                {
                    CompanyId = company.Id,
                    Sale = saleVarejo,
                    Model = InvoiceModel.NFCeModel65,
                    Series = 1,
                    Number = 1,
                    AccessKey = "12345678901234567890123456789012345678901111",
                    Status = InvoiceStatus.Authorized,
                    AuthorizedAt = DateTime.UtcNow
                };

                invoiceVarejo.Events.Add(new InvoiceEvent
                {
                    EventType = "Authorization",
                    Status = "Authorized",
                    Protocol = "202600000000001",
                    OccurredAt = DateTime.UtcNow
                });

                // ===== VENDA ATACADO (NF-e) =====
                var saleAtacado = new Sale
                {
                    CompanyId = company.Id,
                    ClientId = clienteAtacado.Id,
                    UserId = vendedor.Id,
                    TotalProducts = (100 * camisetaAtacado) + (5 * notebookAtacado),
                    TotalDiscount = 0m,
                    TotalPaid = (100 * camisetaAtacado) + (5 * notebookAtacado),
                    Status = SaleStatus.Completed,
                    FiscalModel = InvoiceModel.NFeModel55,
                    SaleDate = DateTime.UtcNow.AddDays(-1)
                };

                saleAtacado.Items.Add(new SaleItem
                {
                    ProductId = products[0].Id, // Camiseta Básica
                    Qty = 100,
                    UnitPrice = camisetaAtacado,
                    Discount = 0m,
                    Cfop = "5102",
                    IcmsValue = 0m,
                    PisValue = 0m,
                    CofinsValue = 0m
                });

                saleAtacado.Items.Add(new SaleItem
                {
                    ProductId = products[4].Id, // Notebook Dell
                    Qty = 5,
                    UnitPrice = notebookAtacado,
                    Discount = 0m,
                    Cfop = "5102",
                    IcmsValue = 0m,
                    PisValue = 0m,
                    CofinsValue = 0m
                });

                saleAtacado.Payments.Add(new Payment
                {
                    Method = PaymentMethod.Cash,
                    Amount = saleAtacado.TotalPaid,
                    PaidAt = DateTime.UtcNow.AddDays(-1)
                });

                var invoiceAtacado = new Invoice
                {
                    CompanyId = company.Id,
                    Sale = saleAtacado,
                    Model = InvoiceModel.NFeModel55,
                    Series = 1,
                    Number = 2,
                    AccessKey = "12345678901234567890123456789012345678902222",
                    Status = InvoiceStatus.Authorized,
                    AuthorizedAt = DateTime.UtcNow.AddDays(-1)
                };

                invoiceAtacado.Events.Add(new InvoiceEvent
                {
                    EventType = "Authorization",
                    Status = "Authorized",
                    Protocol = "202600000000002",
                    OccurredAt = DateTime.UtcNow.AddDays(-1)
                });

                db.Sales.Add(saleVarejo);
                db.Sales.Add(saleAtacado);
                db.Invoices.Add(invoiceVarejo);
                db.Invoices.Add(invoiceAtacado);
                await db.SaveChangesAsync();
            }
        }
    }
}
