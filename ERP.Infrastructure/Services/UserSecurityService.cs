using ERP.Core.Services;
using ERP.Infrastructure.Data;
using ERP.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services;

public sealed class UserSecurityService : IUserSecurityService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionService _sessions;

    public UserSecurityService(ApplicationDbContext db, ISessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public Task<bool> ChangePasswordAsync(int userId, string password, CancellationToken cancellationToken = default) =>
        UpdateAsync(userId, user => user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), "password_changed", cancellationToken);

    public Task<bool> ChangeRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default) =>
        UpdateAsync(userId, user => user.Role = role, "role_changed", cancellationToken);

    public Task<bool> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
        UpdateAsync(userId, user => user.IsActive = isActive, isActive ? "user_activated" : "user_deactivated", cancellationToken);

    private async Task<bool> UpdateAsync(int userId, Action<ERP.Core.Entities.User> update, string reason, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return false;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            update(user);
            user.SessionVersion++;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _sessions.RevokeAllForUserAsync(userId, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        return true;
    }
}
