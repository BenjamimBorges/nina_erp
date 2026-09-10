using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;
namespace NinaERP.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CashSessionId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal TotalProducts { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalPaid { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Open;
    public InvoiceModel? FiscalModel { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public DateTime? SuspendedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public Company? Company { get; set; }
    public Client? Client { get; set; }
    public CashSession? CashSession { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
    public ICollection<AccountReceivable> Receivables { get; set; } = new List<AccountReceivable>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
