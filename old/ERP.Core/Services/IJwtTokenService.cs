using ERP.Core.Entities;

namespace ERP.Core.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
