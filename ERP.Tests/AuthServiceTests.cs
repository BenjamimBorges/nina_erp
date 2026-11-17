using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Infrastructure.Services;
using ERP.Shared.Dtos;
using ERP.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ERP.Tests;

public sealed class AuthServiceTests
{
    private const string Key = "unit-test-signing-key-with-at-least-32-characters";

    [Fact]
    public async Task Authenticate_verifies_hash_and_returns_expiring_role_token()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), Configuration());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "correct-password",
            CompanyId = user.CompanyId
        });

        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrWhiteSpace();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.Should().Contain(x => x.Type == ClaimTypes.Role && x.Value == "Admin");
        token.Claims.Should().Contain(x => x.Type == "companyId" && x.Value == "42");
        token.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Authenticate_rejects_wrong_password_without_token()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), Configuration());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "wrong-password",
            CompanyId = user.CompanyId
        });

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_rejects_user_from_another_company()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), Configuration());

        var result = await service.AuthenticateAsync(new LoginRequestDto
        {
            Username = user.Username,
            Password = "correct-password",
            CompanyId = user.CompanyId + 1
        });

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_rejects_missing_company()
    {
        var user = User("correct-password");
        var service = new AuthService(new FakeUserRepository(user), Configuration());

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
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        FullName = "Admin",
        Role = UserRole.Admin,
        CompanyId = 42,
        Company = new Company { Name = "Test" }
    };

    private static IConfiguration Configuration() => new ConfigurationManager
    {
        ["Jwt:Key"] = Key,
        ["Jwt:Issuer"] = "NinaERP.Tests",
        ["Jwt:Audience"] = "NinaERP.Tests.Client",
        ["Jwt:ExpirationMinutes"] = "30"
    };

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(username == user.Username ? user : null);

        public Task<User?> GetByUsernameAndCompanyAsync(string username, int companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(username == user.Username && companyId == user.CompanyId ? user : null);

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id ? user : null);

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(new[] { user });

        public Task AddAsync(User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Remove(User entity) { }
    }
}
