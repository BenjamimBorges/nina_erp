using ERP.Core.Entities;
using ERP.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Data
{
    public static class DbSeeder
    {
        /// <summary>
        /// Cria empresa demo e usuário admin padrão se ainda não existirem.
        /// Requer que o schema SQL já tenha sido aplicado (001_initial_schema.sql).
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Empresa demo
            var company = await context.Companies
                .FirstOrDefaultAsync(c => c.TaxId == "00.000.000/0001-00");

            if (company == null)
            {
                company = new Company
                {
                    Name = "Empresa Demo",
                    TaxId = "00.000.000/0001-00",
                    Address = "Endereço Demo, 123"
                };
                context.Companies.Add(company);
                await context.SaveChangesAsync();
            }

            // Usuário admin
            var adminExists = await context.Users.AnyAsync(u => u.Username == "admin");
            if (!adminExists)
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Administrador",
                    Role = UserRole.Admin,
                    CompanyId = company.Id
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
