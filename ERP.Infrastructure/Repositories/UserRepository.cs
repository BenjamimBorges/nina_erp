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
            throw new InvalidOperationException("Tenant scope is required for user queries.");
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Tenant scope is required for user queries.");
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Tenant scope is required for user queries.");
        }

        public async Task<User?> GetByUsernameAndCompanyAsync(string username, int companyId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Username == username && x.CompanyId == companyId, cancellationToken);
        }

        public async Task<User?> GetByCnpjAndUsernameAsync(string cnpj, string normalizedUsername, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users.Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Company!.TaxId == cnpj && x.Company.IsActive &&
                    x.NormalizedUsername == normalizedUsername && x.IsActive, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllForCompanyAsync(int companyId, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(x => x.Company)
                .Where(x => x.CompanyId == companyId).ToListAsync(cancellationToken);

        public async Task<User?> GetByIdForCompanyAsync(int id, int companyId, CancellationToken cancellationToken = default) =>
            await _dbContext.Users.Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);

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
