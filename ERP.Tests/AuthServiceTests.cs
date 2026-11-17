using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Services;
using ERP.Shared.Dtos;
using ERP.Shared.Enums;
using FluentAssertions;
using Xunit;

namespace ERP.Tests;

public sealed class AuthServiceTests
{
    private const string Key = "unit-test-signing-key-with-at-least-32-characters";

    [Fact]
    public void Cnpj_normalizer_rejects_invalid_check_digit()
    {
        ERP.Core.Services.CnpjNormalizer.Normalize("11.222.333/0001-80").Should().BeNull();
        ERP.Core.Services.CnpjNormalizer.Normalize("11.222.333/0001-81").Should().Be("11222333000181");
    }

    [Fact]
    public void Cnpj_normalizer_accepts_official_alphanumeric_shape()
    {
        ERP.Core.Services.CnpjNormalizer.Normalize("00.000.000/E08G-12").Should().Be("00000000E08G12");
    }

    [Fact]
    public async Task Authenticate_verifies_hash_and_creates_server_session()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), new FakeSessionService());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "correct-password",
            Cnpj = "11.222.333/0001-81"
        });

        result.Success.Should().BeTrue();
        result.Token.Should().BeNull();
        result.SessionToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Authenticate_rejects_wrong_password_without_token()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), new FakeSessionService());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "wrong-password",
            Cnpj = "11.222.333/0001-81"
        });

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_rejects_user_from_another_company()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), new FakeSessionService());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "correct-password",
            Cnpj = "11.222.333/0001-82"
        });

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_rejects_missing_company()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), new FakeSessionService());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "correct-password"
        });

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    private static User User(string password) => new()
    {
        Id = 7,
        Username = "admin",
        NormalizedUsername = "ADMIN",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        FullName = "Admin",
        Role = UserRole.Admin,
        CompanyId = 42,
        Company = new Company { Name = "Test", TaxId = "11222333000181", IsActive = true }
    };

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(username == user.Username ? user : null);

        public Task<User?> GetByUsernameAndCompanyAsync(string username, int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(username == user.Username && companyId == user.CompanyId ? user : null);

        public Task<User?> GetByCnpjAndUsernameAsync(string cnpj, string normalizedUsername, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(cnpj == user.Company?.TaxId && normalizedUsername == user.NormalizedUsername ? user : null);

        public Task<IReadOnlyList<User>> GetAllForCompanyAsync(int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(companyId == user.CompanyId ? new[] { user } : Array.Empty<User>());

        public Task<User?> GetByIdForCompanyAsync(int id, int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id && companyId == user.CompanyId ? user : null);

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id ? user : null);

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(new[] { user });

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Remove(User entity) { }
    }

    private sealed class FakeSessionService : ERP.Core.Services.ISessionService
    {
        public Task<ERP.Core.Services.CreatedSession> CreateAsync(User user, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ERP.Core.Services.CreatedSession("session-token", user));
        public Task<User?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task RevokeAsync(string rawToken, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RevokeAllForUserAsync(int userId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
