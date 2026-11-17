using ERP.Core.Entities;

namespace ERP.Core.Services;

public sealed record CreatedSession(string RawToken, User User);

public interface ISessionService
{
    Task<CreatedSession> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string rawToken, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(int userId, string reason, CancellationToken cancellationToken = default);
}
