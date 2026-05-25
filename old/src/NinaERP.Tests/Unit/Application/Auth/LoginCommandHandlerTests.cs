using FluentAssertions;
using NSubstitute;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Application.Features.Auth.Commands.Login;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using Xunit;

namespace NinaERP.Tests.Unit.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();

    private LoginCommandHandler CreateHandler() => new(_userRepo, _jwt);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var password = "senha123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = hash,
            FullName = "Administrador",
            Role = UserRole.Admin
        };

        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _jwt.GenerateToken(user).Returns("jwt_token_mock");

        var handler = CreateHandler();
        var command = new LoginCommand("admin", password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Token.Should().Be("jwt_token_mock");
        result.Username.Should().Be("admin");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var command = new LoginCommand("naoexiste", "qualquer");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correta")
        };
        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var command = new LoginCommand("admin", "errada");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
