using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;

namespace ERP.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IUserRepository? _userRepository;
        private IClientRepository? _clientRepository;
        private IProductRepository? _productRepository;
        private IStockRepository? _stockRepository;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IUserRepository Users => _userRepository ??= new UserRepository(_dbContext);

        public IClientRepository Clients => _clientRepository ??= new ClientRepository(_dbContext);
        public IProductRepository Products => _productRepository ??= new ProductRepository(_dbContext);
        public IStockRepository Stocks => _stockRepository ??= new StockRepository(_dbContext);

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
