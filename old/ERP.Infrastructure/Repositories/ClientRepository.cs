using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ClientRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Client entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<Client>().AddAsync(entity, cancellationToken);
        }

        public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Client>().Include(x => x.Company).ToListAsync(cancellationToken);
        }

        public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Client>().Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<Client?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Client>().Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Document == document, cancellationToken);
        }

        public void Remove(Client entity)
        {
            _dbContext.Set<Client>().Remove(entity);
        }

        public void Update(Client entity)
        {
            _dbContext.Set<Client>().Update(entity);
        }
    }
}
