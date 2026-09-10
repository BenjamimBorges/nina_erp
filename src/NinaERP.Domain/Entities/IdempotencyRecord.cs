using NinaERP.Domain.Common;

namespace NinaERP.Domain.Entities;

public sealed class IdempotencyRecord : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string? ResultJson { get; set; }
    public DateTime ExpiresAt { get; set; }
}
