using BCrypt.Net;
using ERP.Core.Interfaces;
using ERP.Core.Services;
using ERP.Shared.Dtos;

namespace ERP.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;

        public AuthService(IUserRepository userRepository, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user == null)
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };

            var token = _jwtService.GenerateToken(user);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso.",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    CompanyName = user.Company?.Name ?? string.Empty
                }
            };
        }
    }
}
