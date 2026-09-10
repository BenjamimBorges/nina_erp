using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class UserAdministrationService(AppDbContext db) : IUserAdministrationService
{
    public async Task<IReadOnlyList<UserAdministrationDto>> ListAsync(Guid companyId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.FullName)
            .Select(x => new UserAdministrationDto(x.Id, x.Username, x.FullName, x.Email, x.Role, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

    public async Task<Guid> CreateAsync(Guid companyId, CreateManagedUserDto request, CancellationToken ct)
    {
        var username = NormalizeUsername(request.Username);
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        ValidatePassword(request.Password);
        if (fullName.Length < 3 || fullName.Length > 160) throw new BusinessException("Nome completo deve possuir entre 3 e 160 caracteres.");
        if (username.Length < 3 || username.Length > 50) throw new BusinessException("Usuário deve possuir entre 3 e 50 caracteres.");
        if (!string.IsNullOrWhiteSpace(email) && (!email.Contains('@') || email.Length > 160)) throw new BusinessException("E-mail inválido.");

        var exists = await db.Users.AnyAsync(x => x.CompanyId == companyId && x.Username.ToLower() == username, ct);
        if (exists) throw new BusinessException("Já existe um usuário com este nome nesta empresa.");

        var user = new User
        {
            CompanyId = companyId,
            Username = username,
            FullName = fullName,
            Email = email,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task ChangeRoleAsync(Guid companyId, Guid actorUserId, Guid userId, UserRole role, CancellationToken ct)
    {
        var user = await FindAsync(companyId, userId, ct);
        if (user.Id == actorUserId && user.Role == UserRole.Admin && role != UserRole.Admin)
            throw new BusinessException("O administrador não pode remover o próprio perfil administrativo.");
        if (user.Role == UserRole.Admin && role != UserRole.Admin)
            await EnsureAnotherActiveAdminAsync(companyId, user.Id, ct);
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid companyId, Guid actorUserId, Guid userId, bool isActive, CancellationToken ct)
    {
        var user = await FindAsync(companyId, userId, ct);
        if (!isActive && user.Id == actorUserId)
            throw new BusinessException("O usuário autenticado não pode desativar a própria conta.");
        if (!isActive && user.Role == UserRole.Admin)
            await EnsureAnotherActiveAdminAsync(companyId, user.Id, ct);
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid companyId, Guid userId, string newPassword, CancellationToken ct)
    {
        ValidatePassword(newPassword);
        var user = await FindAsync(companyId, userId, ct);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<User> FindAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.CompanyId == companyId, ct)
        ?? throw new BusinessException("Usuário não encontrado.");

    private async Task EnsureAnotherActiveAdminAsync(Guid companyId, Guid excludedUserId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.CompanyId == companyId && x.Id != excludedUserId && x.IsActive && x.Role == UserRole.Admin, ct))
            throw new BusinessException("A empresa deve manter ao menos um administrador ativo.");
    }

    private static string NormalizeUsername(string value) => value.Trim().ToLowerInvariant();

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new BusinessException("A senha deve ter pelo menos 12 caracteres, com maiúscula, minúscula, número e símbolo.");
    }
}
