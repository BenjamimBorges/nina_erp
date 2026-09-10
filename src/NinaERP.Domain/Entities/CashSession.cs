using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class CashSession : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid CashRegisterId { get; set; }
    public Guid UserId { get; set; }
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public decimal OpeningAmount { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal? CountedAmount { get; set; }
    public decimal? Difference { get; set; }
    public Guid? DifferenceAuthorizedByUserId { get; set; }
    public string? ClosingNotes { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public long Version { get; set; }
    public Company? Company { get; set; }
    public CashRegister? CashRegister { get; set; }
    public ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
}
