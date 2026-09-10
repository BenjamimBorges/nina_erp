using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class FinancialSettlement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public FinancialTitleType TitleType { get; set; }
    public Guid TitleId { get; set; }
    public Guid UserId { get; set; }
    public FinancialSettlementType Type { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal Interest { get; set; }
    public decimal Penalty { get; set; }
    public decimal Discount { get; set; }
    public string? ExternalReference { get; set; }
    public Guid? ReversesSettlementId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public FinancialTitle? Title { get; set; }
}
