using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record PurchaseItemInput(Guid ProductId, decimal Quantity, decimal UnitCost, decimal Discount = 0, decimal Taxes = 0);
public sealed record PurchaseReceiptItemInput(Guid ProductId, decimal Quantity);
public sealed record PurchaseOrderDto(Guid Id, string Number, Guid SupplierId, string SupplierName, Guid WarehouseId, PurchaseOrderStatus Status, decimal Total, DateTime OrderedAt, DateTime? ExpectedAt, DateTime? ReceivedAt);
public sealed record FinancialTitleDto(Guid Id, FinancialTitleType Type, string DocumentNumber, string Description, decimal OriginalAmount, decimal OpenAmount, decimal PaidAmount, DateTime DueDate, FinancialTitleStatus Status, string OriginType, Guid? OriginId);
public sealed record FinancialSettlementDto(Guid Id, FinancialTitleType TitleType, Guid TitleId, FinancialSettlementType Type, decimal Amount, decimal Interest, decimal Penalty, decimal Discount, DateTime OccurredAt, Guid? ReversesSettlementId);

public interface IPurchaseService
{
    Task<PurchaseOrderDto> CreateAsync(Guid companyId, Guid userId, Guid supplierId, Guid warehouseId, string number, IReadOnlyList<PurchaseItemInput> items, DateTime? expectedAt, string? notes, CancellationToken ct);
    Task<PurchaseOrderDto> ReceiveAsync(Guid companyId, Guid userId, Guid purchaseOrderId, string idempotencyKey, IReadOnlyList<PurchaseReceiptItemInput>? items, DateTime? payableDueDate, CancellationToken ct);
    Task<IReadOnlyList<PurchaseOrderDto>> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

public interface IFinanceService
{
    Task<IReadOnlyList<FinancialTitleDto>> GetPayablesAsync(Guid companyId, DateTime? dueFrom, DateTime? dueTo, CancellationToken ct);
    Task<IReadOnlyList<FinancialTitleDto>> GetReceivablesAsync(Guid companyId, DateTime? dueFrom, DateTime? dueTo, CancellationToken ct);
    Task<FinancialSettlementDto> SettleAsync(Guid companyId, Guid userId, FinancialTitleType titleType, Guid titleId, decimal amount, decimal interest, decimal penalty, decimal discount, PaymentMethod? paymentMethod, string? externalReference, string idempotencyKey, CancellationToken ct);
    Task<FinancialSettlementDto> ReverseSettlementAsync(Guid companyId, Guid userId, Guid settlementId, string reason, string idempotencyKey, CancellationToken ct);
}
