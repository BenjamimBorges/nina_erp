namespace ERP.Core.Entities
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int SessionVersion { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime AbsoluteExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }

        public User? User { get; set; }
        public Company? Company { get; set; }
    }
}
