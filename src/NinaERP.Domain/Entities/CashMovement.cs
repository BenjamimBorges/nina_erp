using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class CashMovement : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid CashSessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? PaymentTransactionId { get; set; }
    public CashMovementType Type { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? AuthorizedByUserId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public CashSession? CashSession { get; set; }
    public Sale? Sale { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}
