using System.Security.Cryptography;
using System.Text;
using ERP.Core.Entities;
using ERP.Core.Services;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    private static readonly TimeSpan IdleLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(8);
    private readonly ApplicationDbContext _db;

    public SessionService(ApplicationDbContext db) => _db = db;

    public async Task<CreatedSession> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        _db.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, CompanyId = user.CompanyId,
            TokenHash = Hash(rawToken), CreatedAt = now, LastActivityAt = now,
            AbsoluteExpiresAt = now.Add(AbsoluteLifetime)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new CreatedSession(rawToken, user);
    }

    public async Task<User?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var session = await _db.UserSessions.Include(x => x.User).ThenInclude(x => x!.Company)
            .SingleOrDefaultAsync(x => x.TokenHash == Hash(rawToken), cancellationToken);
        if (session?.User is null || session.RevokedAt is not null || !session.User.IsActive || session.User.Company?.IsActive != true)
            return null;

        var now = DateTime.UtcNow;
        if (now >= session.AbsoluteExpiresAt || now - session.LastActivityAt >= IdleLifetime)
        {
            session.RevokedAt = now;
            session.RevocationReason = "expired";
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        session.LastActivityAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return session.User;
    }

    public async Task RevokeAsync(string rawToken, string reason, CancellationToken cancellationToken = default)
    {
        var session = await _db.UserSessions.SingleOrDefaultAsync(x => x.TokenHash == Hash(rawToken), cancellationToken);
        if (session is null || session.RevokedAt is not null) return;
        session.RevokedAt = DateTime.UtcNow;
        session.RevocationReason = reason;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _db.UserSessions.Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAt, now)
                .SetProperty(x => x.RevocationReason, reason), cancellationToken);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
