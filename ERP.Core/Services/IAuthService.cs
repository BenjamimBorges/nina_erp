using ERP.Shared.Dtos;

namespace ERP.Core.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    }
}
