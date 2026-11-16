using System.Text.RegularExpressions;
using ERP.Core.Entities;
using ERP.Core.Services;
using ERP.Infrastructure.Data;
using ERP.Shared.Dtos;
using ERP.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services
{
    public sealed class CompanyRegistrationService : ICompanyRegistrationService
    {
        private static readonly Regex UsernamePattern =
            new("^[A-Za-z0-9._-]{3,100}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly ApplicationDbContext _dbContext;

        public CompanyRegistrationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CompanyRegistrationResult> RegisterAsync(
            CompanyRegistrationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = Validate(request, out var cnpj, out var username, out var companyName, out var fullName);
            if (errors.Count > 0)
                return CompanyRegistrationResult.Invalid(errors);

            if (await _dbContext.Companies.AnyAsync(x => x.TaxId == cnpj, cancellationToken))
                return CompanyRegistrationResult.Duplicate();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var company = new Company
                {
                    Name = companyName,
                    TaxId = cnpj!,
                    IsActive = true
                };
                var administrator = new User
                {
                    Company = company,
                    Username = username!,
                    NormalizedUsername = username!.ToUpperInvariant(),
                    FullName = fullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.Admin,
                    IsActive = true,
                    SessionVersion = 0
                };

                await _dbContext.Companies.AddAsync(company, cancellationToken);
                await _dbContext.Users.AddAsync(administrator, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return CompanyRegistrationResult.Created();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return CompanyRegistrationResult.Duplicate();
            }
        }

        private static Dictionary<string, string[]> Validate(
            CompanyRegistrationRequestDto request,
            out string? cnpj,
            out string? username,
            out string companyName,
            out string fullName)
        {
            var errors = new Dictionary<string, string[]>();
            companyName = request.CompanyName?.Trim() ?? string.Empty;
            fullName = request.AdminFullName?.Trim() ?? string.Empty;
            username = request.AdminUsername?.Trim() ?? string.Empty;
            cnpj = CnpjNormalizer.Normalize(request.Cnpj);
            var password = request.Password ?? string.Empty;

            if (companyName.Length is < 2 or > 200)
                errors["companyName"] = ["Informe o nome da empresa entre 2 e 200 caracteres."];
            if (cnpj is null)
                errors["cnpj"] = ["Informe um CNPJ válido, inclusive no formato alfanumérico oficial."];
            if (!UsernamePattern.IsMatch(username))
                errors["adminUsername"] = ["Use de 3 a 100 caracteres: letras, números, ponto, hífen ou sublinhado."];
            if (fullName.Length is < 2 or > 200)
                errors["adminFullName"] = ["Informe o nome do administrador entre 2 e 200 caracteres."];
            if (!IsStrongPassword(password))
                errors["password"] = ["A senha deve ter no mínimo 5 caracteres."];
            if (!string.Equals(password, request.ConfirmPassword ?? string.Empty, StringComparison.Ordinal))
                errors["confirmPassword"] = ["A confirmação da senha não confere."];

            return errors;
        }

        private static bool IsStrongPassword(string password) => password.Length >= 5;
    }
}
