using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public StockRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Stock entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Set<Stock>().AddAsync(entity, cancellationToken);
        }

        public async Task<IReadOnlyList<Stock>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Stock>().Include(x => x.Product).ToListAsync(cancellationToken);
        }

        public async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Stock>().Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<Stock?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<Stock>().Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        }

        public void Remove(Stock entity)
        {
            _dbContext.Set<Stock>().Remove(entity);
        }

        public void Update(Stock entity)
        {
            _dbContext.Set<Stock>().Update(entity);
        }
    }
}
