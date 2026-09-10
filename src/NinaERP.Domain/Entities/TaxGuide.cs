using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class TaxGuide : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? AccountPayableId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public int ReferenceYear { get; set; }
    public int ReferenceMonth { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? RevenueCode { get; set; }
    public string? DigitableLine { get; set; }
    public string? DocumentPath { get; set; }
    public string? ProofPath { get; set; }
    public TaxGuideStatus Status { get; set; } = TaxGuideStatus.Open;
    public string? OriginDescription { get; set; }
    public Company? Company { get; set; }
    public AccountPayable? AccountPayable { get; set; }
}
