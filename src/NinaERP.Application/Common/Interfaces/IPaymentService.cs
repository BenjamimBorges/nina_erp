using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record PaymentRequestDto(
    PaymentMethod Method,
    decimal Amount,
    string IdempotencyKey,
    string Provider = "manual",
    string? ExternalTransactionId = null,
    string? Nsu = null,
    string? AuthCode = null,
    string? PixTxId = null,
    DateTime? DueDate = null);

public sealed record PaymentTransactionDto(
    Guid Id, Guid SaleId, PaymentMethod Method, decimal Amount, decimal RefundedAmount,
    PaymentTransactionStatus Status, PaymentConfirmationSource ConfirmationSource,
    string Provider, string IdempotencyKey, string? ExternalTransactionId,
    string? FailureCode, string? FailureMessage, DateTime CreatedAt,
    DateTime? CapturedAt, DateTime? RefundedAt);

public sealed record PaymentProviderResult(
    PaymentProviderOperationStatus Status,
    string? ExternalTransactionId = null,
    string? Nsu = null,
    string? AuthCode = null,
    string? PixTxId = null,
    string? FailureCode = null,
    string? FailureMessage = null,
    string? RawPayloadHash = null,
    decimal? RefundedAmount = null);

public sealed record VerifiedPaymentWebhook(
    Guid CompanyId,
    Guid TransactionId,
    PaymentProviderResult Result);

public interface IPaymentProviderAdapter
{
    string ProviderName { get; }
    bool Supports(PaymentMethod method);
    Task<PaymentProviderResult> StartAsync(Guid companyId, Guid saleId, decimal amount, string idempotencyKey, CancellationToken ct);
    Task<PaymentProviderResult> RefundAsync(Guid companyId, string externalTransactionId, decimal amount, string idempotencyKey, CancellationToken ct);
}

/// <summary>
/// Optional contract for providers that can cryptographically verify incoming webhooks.
/// An endpoint must never trust provider payloads that have not passed this adapter.
/// </summary>
public interface IPaymentWebhookAdapter : IPaymentProviderAdapter
{
    Task<VerifiedPaymentWebhook> VerifyAndParseWebhookAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct);
}

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentTransactionDto>> RegisterAsync(Guid companyId, Guid userId, Guid saleId, Guid cashSessionId, IReadOnlyList<PaymentRequestDto> payments, CancellationToken ct);
    Task<PaymentTransactionDto> ConfirmExternalAsync(Guid companyId, Guid transactionId, PaymentProviderResult result, PaymentConfirmationSource source, CancellationToken ct);
    Task<PaymentTransactionDto> RefundAsync(Guid companyId, Guid userId, Guid transactionId, decimal amount, string idempotencyKey, CancellationToken ct);
    Task<PaymentTransactionDto> ConfirmRefundAsync(Guid companyId, Guid transactionId, decimal amount, string idempotencyKey, PaymentProviderResult result, PaymentConfirmationSource source, CancellationToken ct);
    Task<IReadOnlyList<PaymentTransactionDto>> GetSaleTransactionsAsync(Guid companyId, Guid saleId, CancellationToken ct);
}
