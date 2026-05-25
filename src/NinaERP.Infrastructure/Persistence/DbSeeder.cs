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
                    PriceSale = 49.90m,
                    PriceMinimum = 39.90m,
                    CostAverage = 25.00m,
                    StockQty = 150,
                    StockMin = 20,
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
                    Name = "Notebook Tecnológico",
                    Description = "Notebook semi-novo com SSD de 256GB",
                    Ncm = "84713012",
                    Cest = "0505050",
                    Unit = "UN",
                    PriceSale = 2299.90m,
                    PriceMinimum = 1999.90m,
                    CostAverage = 1800.00m,
                    StockQty = 8,
                    StockMin = 10,
                    Brand = "Nina",
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
                        Qty = 100,
                        CostUnit = 24.50m,
                        OriginType = "Ajuste inicial",
                        Notes = "Entrada inicial de estoque"
                    },
                    new StockMovement
                    {
                        CompanyId = company.Id,
                        ProductId = products[4].Id,
                        Type = StockMovementType.Entry,
                        Qty = 8,
                        CostUnit = 1800.00m,
                        OriginType = "Compra demo",
                        Notes = "Entrada de notebook"
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
                var client = await db.Clients.FirstAsync(c => c.CompanyId == company.Id);
                var sale = new Sale
                {
                    CompanyId = company.Id,
                    ClientId = client.Id,
                    UserId = admin.Id,
                    TotalProducts = 149.70m,
                    TotalDiscount = 0m,
                    TotalPaid = 149.70m,
                    Status = SaleStatus.Completed,
                    FiscalModel = InvoiceModel.NFCeModel65,
                    SaleDate = DateTime.UtcNow
                };

                sale.Items.Add(new SaleItem
                {
                    ProductId = products[0].Id,
                    Qty = 3,
                    UnitPrice = 49.90m,
                    Discount = 0m,
                    Cfop = "5102",
                    IcmsValue = 0m,
                    PisValue = 0m,
                    CofinsValue = 0m
                });

                sale.Payments.Add(new Payment
                {
                    Method = PaymentMethod.Cash,
                    Amount = 149.70m,
                    PaidAt = DateTime.UtcNow
                });

                var invoice = new Invoice
                {
                    CompanyId = company.Id,
                    Sale = sale,
                    Model = InvoiceModel.NFCeModel65,
                    Series = 1,
                    Number = 1,
                    AccessKey = "12345678901234567890123456789012345678901234",
                    Status = InvoiceStatus.Authorized,
                    AuthorizedAt = DateTime.UtcNow
                };

                invoice.Events.Add(new InvoiceEvent
                {
                    EventType = "Authorization",
                    Status = "Authorized",
                    Protocol = "202600000000000",
                    OccurredAt = DateTime.UtcNow
                });

                db.Sales.Add(sale);
                db.Invoices.Add(invoice);
                await db.SaveChangesAsync();
            }
        }
    }
}
