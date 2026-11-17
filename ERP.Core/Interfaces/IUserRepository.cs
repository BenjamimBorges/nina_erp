using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAndCompanyAsync(string username, int companyId, CancellationToken cancellationToken = default);
    }
}
