using NinaERP.Domain.Entities;

namespace NinaERP.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
