using ERP.Core.Entities;

namespace ERP.Core.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
