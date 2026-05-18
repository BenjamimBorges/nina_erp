using ERP.Core.Interfaces;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;

namespace ERP.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IUserRepository? _userRepository;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IUserRepository Users => _userRepository ??= new UserRepository(_dbContext);

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
