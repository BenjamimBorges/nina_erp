using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Department>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await _set
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.Products)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<Department?> GetByNameAsync(Guid companyId, string name, CancellationToken ct = default) =>
        await _set
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Name == name, ct);
}
