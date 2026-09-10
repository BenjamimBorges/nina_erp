using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetForLoginAsync(string username, string companyCnpj, CancellationToken ct = default)
    {
        var normalizedUser = username.Trim().ToLowerInvariant();
        var normalizedCnpj = new string(companyCnpj.Where(char.IsDigit).ToArray());
        if (normalizedCnpj.Length != 14) return null;
        var matches = await _set.Include(x => x.Company)
            .Where(x => x.Username.ToLower() == normalizedUser && x.Company != null &&
                x.Company.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "") == normalizedCnpj && x.IsActive)
            .Take(2)
            .ToListAsync(ct);

        // Legacy databases may contain case-only duplicate usernames. Never guess the tenant identity.
        return matches.Count == 1 ? matches[0] : null;
    }

    public async Task<IReadOnlyList<User>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);
}
