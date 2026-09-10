using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class AssistantInteraction : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Intent { get; set; } = string.Empty;
    public string QuestionHash { get; set; } = string.Empty;
    public string ResponseSummary { get; set; } = string.Empty;
    public bool SensitiveActionRequested { get; set; }
    public bool Blocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
