using ERP.Core.Interfaces;
using ERP.Core.Services;
using ERP.Shared.Dtos;

namespace ERP.Infrastructure.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionService _sessions;

        public AuthService(IUserRepository userRepository, ISessionService sessions)
        {
            _userRepository = userRepository;
            _sessions = sessions;
        }

        public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var cnpj = CnpjNormalizer.Normalize(request.Cnpj);
            var username = request.Username.Trim().ToUpperInvariant();
            if (cnpj is null || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
                return Invalid();

            var user = await _userRepository.GetByCnpjAndUsernameAsync(cnpj, username, cancellationToken);
            if (user is null) return Invalid();
            try
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return Invalid();
            }
            catch (ArgumentException) { return Invalid(); }

            var session = await _sessions.CreateAsync(user, cancellationToken);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso.",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    CompanyName = user.Company?.Name ?? string.Empty
                },
                SessionToken = session.RawToken
            };
        }

        private static LoginResponseDto Invalid() => new()
        {
            Success = false,
            Message = "CNPJ, usuário ou senha inválidos."
        };
    }
}
