using ERP.Core.Entities;

namespace ERP.Core.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAndCompanyAsync(string username, int companyId, CancellationToken cancellationToken = default);
        Task<User?> GetByCnpjAndUsernameAsync(string cnpj, string normalizedUsername, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<User>> GetAllForCompanyAsync(int companyId, CancellationToken cancellationToken = default);
        Task<User?> GetByIdForCompanyAsync(int id, int companyId, CancellationToken cancellationToken = default);
    }
}
