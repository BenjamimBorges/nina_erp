using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;

public class ClientRepository : BaseRepository<Client>, IClientRepository
{
    public ClientRepository(AppDbContext db) : base(db) { }

    public async Task<Client?> GetByDocumentAsync(Guid companyId, string document, CancellationToken ct = default) =>
        await _set.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Document == document && x.IsActive, ct);

    public async Task<IReadOnlyList<Client>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Client>> SearchAsync(Guid companyId, string term, CancellationToken ct = default) =>
        await _set.Where(x => x.CompanyId == companyId && x.IsActive &&
            (x.Name.Contains(term) || x.Document.Contains(term) || x.Phone.Contains(term)))
            .OrderBy(x => x.Name).Take(100).ToListAsync(ct);

    public async Task<int> CountAsync(Guid companyId, CancellationToken ct = default) =>
        await _set.CountAsync(x => x.CompanyId == companyId && x.IsActive, ct);
}
