using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;

namespace NinaERP.Infrastructure.Persistence.Repositories;
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await _set.Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Username == username && x.IsActive, ct);
}
