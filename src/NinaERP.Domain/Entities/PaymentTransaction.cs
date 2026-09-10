using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public sealed class PaymentTransaction : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid SaleId { get; set; }
    public Guid? CashSessionId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public PaymentConfirmationSource ConfirmationSource { get; set; } = PaymentConfirmationSource.Manual;
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Provider { get; set; } = "manual";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public string? Nsu { get; set; }
    public string? AuthCode { get; set; }
    public string? PixTxId { get; set; }
    public string? ProviderPayloadHash { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public long Version { get; set; }
    public Sale? Sale { get; set; }
    public CashSession? CashSession { get; set; }
    public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}
