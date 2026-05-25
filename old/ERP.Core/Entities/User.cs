using ERP.Shared.Enums;

namespace ERP.Core.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
