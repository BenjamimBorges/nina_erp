using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<Product>().AddAsync(entity, cancellationToken);
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Product>().Include(x => x.Company).ToListAsync(cancellationToken);
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Product>().Include(x => x.Company).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Product>().Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.Sku == sku, cancellationToken);
        }

        public void Remove(Product entity)
        {
            _dbContext.Set<Product>().Remove(entity);
        }

        public void Update(Product entity)
        {
            _dbContext.Set<Product>().Update(entity);
        }
    }
}
