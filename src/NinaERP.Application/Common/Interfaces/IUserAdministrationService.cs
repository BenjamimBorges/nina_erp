using NinaERP.Domain.Entities;

namespace NinaERP.Application.Common.Interfaces;

public sealed record UserAdministrationDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateManagedUserDto(
    string Username,
    string FullName,
    string Email,
    string Password,
    UserRole Role);

public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserAdministrationDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<Guid> CreateAsync(Guid companyId, CreateManagedUserDto request, CancellationToken ct);
    Task ChangeRoleAsync(Guid companyId, Guid actorUserId, Guid userId, UserRole role, CancellationToken ct);
    Task SetActiveAsync(Guid companyId, Guid actorUserId, Guid userId, bool isActive, CancellationToken ct);
    Task ResetPasswordAsync(Guid companyId, Guid userId, string newPassword, CancellationToken ct);
}
