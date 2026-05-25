using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public CompanyRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Company entity, CancellationToken cancellationToken = default)
            => await _dbContext.Companies.AddAsync(entity, cancellationToken);

        public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _dbContext.Companies.ToListAsync(cancellationToken);

        public async Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _dbContext.Companies
                .Include(c => c.Users)
                .Include(c => c.Clients)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task<Company?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken = default)
            => await _dbContext.Companies.FirstOrDefaultAsync(c => c.TaxId == taxId, cancellationToken);

        public void Update(Company entity)
            => _dbContext.Companies.Update(entity);

        public void Remove(Company entity)
            => _dbContext.Companies.Remove(entity);
    }
}
