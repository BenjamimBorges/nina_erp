using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetForLoginAsync(string username, string companyCnpj, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
}
