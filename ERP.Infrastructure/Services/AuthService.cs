using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Core.Services;
using ERP.Shared.Dtos;

namespace ERP.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user == null)
            {
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };
            }

            if (user.PasswordHash != request.Password)
            {
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };
            }

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
                Token = string.Empty
            };
        }
    }
}
