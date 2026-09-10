using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class ApprovalRequest : BaseEntity
{
    public Guid CompanyId { get; set; }
    public ApprovalType Type { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public UserRole RequiredRole { get; set; } = UserRole.Manager;
    public string Reason { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionNotes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long Version { get; set; }
}
