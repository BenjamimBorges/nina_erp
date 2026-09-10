using NinaERP.Domain.Enums;
namespace NinaERP.Contracts.Requests;
public record CreateSaleItemRequest(Guid ProductId, decimal Qty, decimal Discount = 0);
public record CreateSaleRequest(Guid CashSessionId, Guid? WarehouseId, string IdempotencyKey, Guid? ClientId, IReadOnlyList<CreateSaleItemRequest> Items);
public record SalePaymentRequest(PaymentMethod Method, decimal Amount, string IdempotencyKey, string Provider = "manual", string? ExternalTransactionId = null, string? Nsu = null, string? AuthCode = null, string? PixTxId = null, DateTime? DueDate = null);
public record FinalizeSaleRequest(Guid CashSessionId, IReadOnlyList<SalePaymentRequest> Payments);
public record CancelSaleRequest(string Reason, string IdempotencyKey);
