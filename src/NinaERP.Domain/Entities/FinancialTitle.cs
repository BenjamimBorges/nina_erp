using NinaERP.Domain.Common;
using NinaERP.Domain.Enums;

namespace NinaERP.Domain.Entities;

public abstract class FinancialTitle : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public FinancialTitleStatus Status { get; set; } = FinancialTitleStatus.Open;
    public int InstallmentNumber { get; set; } = 1;
    public int InstallmentCount { get; set; } = 1;
    public string OriginType { get; set; } = string.Empty;
    public Guid? OriginId { get; set; }
    public string? Notes { get; set; }
    public long Version { get; set; }
    public Company? Company { get; set; }
    public ICollection<FinancialSettlement> Settlements { get; set; } = new List<FinancialSettlement>();
}

public sealed class AccountPayable : FinancialTitle
{
    public Guid? SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Supplier? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

public sealed class AccountReceivable : FinancialTitle
{
    public Guid? ClientId { get; set; }
    public Guid? SaleId { get; set; }
    public Client? Client { get; set; }
    public Sale? Sale { get; set; }
}
