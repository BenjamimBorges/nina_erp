using ERP.Shared.Enums;

namespace ERP.Core.Services;

public interface IUserSecurityService
{
    Task<bool> ChangePasswordAsync(int userId, string password, CancellationToken cancellationToken = default);
    Task<bool> ChangeRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
}
