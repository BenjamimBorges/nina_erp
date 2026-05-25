using NinaERP.Domain.Common;
namespace NinaERP.Domain.Entities;
public enum UserRole { Admin, Manager, Cashier, Stock }
public class User : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public Company? Company { get; set; }
}
