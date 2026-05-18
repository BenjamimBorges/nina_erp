using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Users.AddAsync(entity, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(x => x.Company).ToListAsync(cancellationToken);
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
        }

        public void Remove(User entity)
        {
            _dbContext.Users.Remove(entity);
        }

        public void Update(User entity)
        {
            _dbContext.Users.Update(entity);
        }
    }
}
