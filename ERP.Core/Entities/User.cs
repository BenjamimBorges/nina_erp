using ERP.Shared.Enums;

namespace ERP.Core.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string NormalizedUsername { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public bool IsActive { get; set; } = true;
        public int SessionVersion { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }
}
