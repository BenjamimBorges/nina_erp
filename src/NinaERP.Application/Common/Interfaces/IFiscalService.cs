using NinaERP.Domain.Enums;

namespace NinaERP.Application.Common.Interfaces;

public sealed record FiscalProviderRequest(
    Guid CompanyId, Guid InvoiceId, Guid SaleId, InvoiceModel Model, FiscalEnvironment Environment,
    int Series, int Number, string IdempotencyKey, string CertificateThumbprint,
    bool AllowContingency, IReadOnlyList<TaxCalculationResult> TaxLines);

public sealed record FiscalProviderResult(
    FiscalProviderOperationStatus Status,
    string? ExternalDocumentId = null,
    string? AccessKey = null,
    string? Protocol = null,
    string? XmlContent = null,
    string? DanfeContentBase64 = null,
    string? RejectionCode = null,
    string? RejectionMessage = null,
    bool IsContingency = false,
    string? ExternalEventId = null);

public interface IFiscalProvider
{
    string Name { get; }
    Task<FiscalProviderResult> IssueAsync(FiscalProviderRequest request, CancellationToken ct);
    Task<FiscalProviderResult> QueryAsync(FiscalProviderRequest request, string externalDocumentId, CancellationToken ct);
    Task<FiscalProviderResult> CancelAsync(FiscalProviderRequest request, string externalDocumentId, string reason, string eventIdempotencyKey, CancellationToken ct);
    Task<FiscalProviderResult> VoidNumberAsync(FiscalProviderRequest request, int fromNumber, int toNumber, string reason, string eventIdempotencyKey, CancellationToken ct);
    Task<FiscalProviderResult> CorrectionLetterAsync(FiscalProviderRequest request, string externalDocumentId, string correction, string eventIdempotencyKey, CancellationToken ct);
}


public sealed record FiscalConfigurationDto(Guid Id, FiscalEnvironment Environment, string Provider, string? CertificateThumbprint, string? ProviderSecretReference, string? NfceCscId, string? NfceCscSecretReference, int NfeSeries, int NfceSeries, bool AllowNfceContingency, bool Enabled);

public sealed record FiscalInvoiceDto(Guid Id, Guid? SaleId, InvoiceModel Model, FiscalEnvironment Environment,
    int Series, int Number, string? AccessKey, InvoiceStatus Status, string Provider, string? Protocol,
    string? RejectionCode, string? RejectionMessage, bool IsContingency, DateTime? AuthorizedAt, DateTime CreatedAt);
public sealed record FiscalDashboardDto(int Authorized, int Pending, int Rejected, int Cancelled, int Contingency);
public sealed record FiscalNumberVoidDto(Guid Id, InvoiceModel Model, FiscalEnvironment Environment, int Series, int FromNumber, int ToNumber, string Reason, FiscalNumberVoidStatus Status, string Provider, string? Protocol, string? RejectionCode, string? RejectionMessage, DateTime CreatedAt, DateTime? CompletedAt);
public sealed record TaxGuideDto(Guid Id, string TaxType, int ReferenceYear, int ReferenceMonth, decimal TotalAmount,
    DateTime DueDate, TaxGuideStatus Status, string? RevenueCode, string? DigitableLine, string? DocumentPath, string? ProofPath, Guid? AccountPayableId);

public interface IFiscalService
{
    Task<FiscalConfigurationDto?> GetConfigurationAsync(Guid companyId, CancellationToken ct);
    Task<FiscalConfigurationDto> SaveConfigurationAsync(Guid companyId, FiscalConfigurationDto configuration, CancellationToken ct);
    Task<FiscalDashboardDto> GetDashboardAsync(Guid companyId, CancellationToken ct);
    Task<IReadOnlyList<FiscalInvoiceDto>> GetInvoicesAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
    Task<FiscalInvoiceDto> IssueAsync(Guid companyId, Guid saleId, InvoiceModel model, string idempotencyKey, bool allowContingency, CancellationToken ct);
    Task<FiscalInvoiceDto> QueryAsync(Guid companyId, Guid invoiceId, CancellationToken ct);
    Task<FiscalInvoiceDto> CancelAsync(Guid companyId, Guid invoiceId, string reason, string idempotencyKey, CancellationToken ct);
    Task<FiscalInvoiceDto> CorrectionLetterAsync(Guid companyId, Guid invoiceId, string correction, string idempotencyKey, CancellationToken ct);
    Task<IReadOnlyList<FiscalNumberVoidDto>> GetNumberVoidsAsync(Guid companyId, CancellationToken ct);
    Task<FiscalNumberVoidDto> VoidNumbersAsync(Guid companyId, InvoiceModel model, int series, int fromNumber, int toNumber, string reason, string idempotencyKey, CancellationToken ct);
    Task<IReadOnlyList<TaxGuideDto>> GetTaxGuidesAsync(Guid companyId, CancellationToken ct);
    Task<TaxGuideDto> CreateTaxGuideAsync(Guid companyId, string taxType, int referenceYear, int referenceMonth, DateTime dueDate, decimal principalAmount, string? revenueCode, string? digitableLine, string? documentPath, Guid? accountPayableId, string? originDescription, CancellationToken ct);
    Task<TaxGuideDto> MarkTaxGuidePaidAsync(Guid companyId, Guid guideId, string proofPath, CancellationToken ct);
}
