using NinaERP.Domain.Entities;
namespace NinaERP.Application.Common.Interfaces;
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}
