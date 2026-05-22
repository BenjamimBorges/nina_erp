using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public class User : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
