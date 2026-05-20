using MediatR;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwt;

    public LoginCommandHandler(IUserRepository users, IJwtTokenGenerator jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(request.Username, ct)
            ?? throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        var token = _jwt.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
