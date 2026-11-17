using ERP.Core.Entities;
using ERP.Core.Interfaces;
using ERP.Core.Services;
using ERP.Shared.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || request.CompanyId <= 0)
            {
                return new LoginResponseDto { Success = false, Message = "Usuário, senha ou empresa inválidos." };
            }

            var user = await _userRepository.GetByUsernameAndCompanyAsync(
                request.Username.Trim(), request.CompanyId, cancellationToken);
            if (user == null)
            {
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };
            }

            var passwordValid = false;
            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (ArgumentException)
            {
                // Hashes legados inválidos falham como credencial inválida.
            }

            if (!passwordValid)
            {
                return new LoginResponseDto { Success = false, Message = "Usuário ou senha inválidos." };
            }

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException("Jwt:Key deve possuir ao menos 32 caracteres.");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("Jwt:Issuer e Jwt:Audience devem estar configurados.");

            var configuredMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes)
                ? minutes
                : 60;
            var expiresAt = DateTime.UtcNow.AddMinutes(Math.Clamp(configuredMinutes, 5, 1440));
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("companyId", user.CompanyId.ToString())
            };
            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: signingCredentials);

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
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }
    }
}
