using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Common;

namespace NinaERP.Infrastructure.Persistence.Repositories;
public class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;
    public BaseRepository(AppDbContext db) { _db = db; _set = db.Set<T>(); }
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _set.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await _set.Where(x => x.IsActive).ToListAsync(ct);
    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _set.AddAsync(entity, ct);
    public void Update(T entity) => _set.Update(entity);
    public void Remove(T entity) { entity.IsActive = false; _set.Update(entity); }
}
