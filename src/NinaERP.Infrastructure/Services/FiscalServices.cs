using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class NotConfiguredFiscalProvider : IFiscalProvider
{
    public string Name => "NotConfigured";
    private static FiscalProviderResult Unsupported() => new(FiscalProviderOperationStatus.Unsupported, RejectionCode: "PROVIDER_NOT_CONFIGURED", RejectionMessage: "Nenhum provedor fiscal homologado foi configurado.");
    public Task<FiscalProviderResult> IssueAsync(FiscalProviderRequest request, CancellationToken ct) => Task.FromResult(Unsupported());
    public Task<FiscalProviderResult> QueryAsync(FiscalProviderRequest request, string externalDocumentId, CancellationToken ct) => Task.FromResult(Unsupported());
    public Task<FiscalProviderResult> CancelAsync(FiscalProviderRequest request, string externalDocumentId, string reason, string eventIdempotencyKey, CancellationToken ct) => Task.FromResult(Unsupported());
    public Task<FiscalProviderResult> VoidNumberAsync(FiscalProviderRequest request, int fromNumber, int toNumber, string reason, string eventIdempotencyKey, CancellationToken ct) => Task.FromResult(Unsupported());
    public Task<FiscalProviderResult> CorrectionLetterAsync(FiscalProviderRequest request, string externalDocumentId, string correction, string eventIdempotencyKey, CancellationToken ct) => Task.FromResult(Unsupported());
}

public sealed class FiscalService(AppDbContext db, ITaxEngineService taxEngine, IEnumerable<IFiscalProvider> providers, Microsoft.Extensions.Configuration.IConfiguration configuration) : IFiscalService
{
    public async Task<FiscalConfigurationDto?> GetConfigurationAsync(Guid companyId, CancellationToken ct)
    {
        var x = await db.FiscalConfigurations.AsNoTracking().FirstOrDefaultAsync(v => v.CompanyId == companyId && v.IsActive, ct);
        return x is null ? null : Map(x);
    }
    public async Task<FiscalConfigurationDto> SaveConfigurationAsync(Guid companyId, FiscalConfigurationDto input, CancellationToken ct)
    {
        if (input.NfeSeries <= 0 || input.NfceSeries <= 0) throw new BusinessException("Séries fiscais devem ser positivas.");
        var x = await db.FiscalConfigurations.FirstOrDefaultAsync(v => v.CompanyId == companyId && v.IsActive, ct);
        if (x is null) { x = new FiscalConfiguration { CompanyId = companyId }; db.FiscalConfigurations.Add(x); }
        x.Environment=input.Environment; x.Provider=input.Provider.Trim(); x.CertificateThumbprint=input.CertificateThumbprint?.Replace(" ", "").ToUpperInvariant();
        x.ProviderSecretReference=input.ProviderSecretReference?.Trim(); x.NfceCscId=input.NfceCscId?.Trim(); x.NfceCscSecretReference=input.NfceCscSecretReference?.Trim();
        x.NfeSeries=input.NfeSeries; x.NfceSeries=input.NfceSeries; x.AllowNfceContingency=input.AllowNfceContingency; x.Enabled=input.Enabled; x.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Map(x);
    }
    public async Task<FiscalDashboardDto> GetDashboardAsync(Guid companyId, CancellationToken ct)
    {
        var q = db.Invoices.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
        return new(await q.CountAsync(x => x.Status == InvoiceStatus.Authorized, ct), await q.CountAsync(x => x.Status is InvoiceStatus.Pending or InvoiceStatus.Processing, ct), await q.CountAsync(x => x.Status is InvoiceStatus.Rejected or InvoiceStatus.Denied, ct), await q.CountAsync(x => x.Status == InvoiceStatus.Cancelled, ct), await q.CountAsync(x => x.IsContingency, ct));
    }

    public async Task<IReadOnlyList<FiscalInvoiceDto>> GetInvoicesAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct) =>
        await db.Invoices.AsNoTracking().Where(x => x.CompanyId == companyId && x.CreatedAt >= from && x.CreatedAt <= to && x.IsActive).OrderByDescending(x => x.CreatedAt).Take(1000)
            .Select(x => new FiscalInvoiceDto(x.Id, x.SaleId, x.Model, x.Environment, x.Series, x.Number, x.AccessKey, x.Status, x.Provider, x.Protocol, x.RejectionCode, x.RejectionMessage, x.IsContingency, x.AuthorizedAt, x.CreatedAt)).ToListAsync(ct);

    public async Task<FiscalInvoiceDto> IssueAsync(Guid companyId, Guid saleId, InvoiceModel model, string idempotencyKey, bool allowContingency, CancellationToken ct)
    {
        idempotencyKey = RequiredIdempotencyKey(idempotencyKey);
        var prior = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey, ct);
        if (prior is not null) return Map(prior);
        var cfg = await db.FiscalConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Enabled && x.IsActive, ct) ?? throw new BusinessException("Configuração fiscal habilitada não encontrada.");
        if (string.IsNullOrWhiteSpace(cfg.CertificateThumbprint)) throw new BusinessException("Configure o certificado no Windows Certificate Store por thumbprint.");
        var provider = providers.FirstOrDefault(x => string.Equals(x.Name, cfg.Provider, StringComparison.OrdinalIgnoreCase)) ?? providers.First(x => x.Name == "NotConfigured");
        var sale = await db.Sales.AsNoTracking().Include(x => x.Client).Include(x => x.Items).ThenInclude(x => x.Product).FirstOrDefaultAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Venda não encontrada.");
        if (sale.Status != SaleStatus.Completed) throw new BusinessException("Documento fiscal só pode ser solicitado após conclusão operacional da venda.");
        if (await db.Invoices.AnyAsync(x => x.CompanyId == companyId && x.SaleId == saleId && x.Status != InvoiceStatus.Cancelled && x.IsActive, ct)) throw new BusinessException("Já existe documento fiscal ativo para esta venda.");
        var company = await db.Companies.AsNoTracking().FirstAsync(x => x.Id == companyId, ct);

        var taxes = new List<TaxCalculationResult>();
        foreach (var item in sale.Items)
        {
            var p = item.Product!;
            taxes.Add(await taxEngine.CalculateAsync(new TaxContext(companyId, p.Id, company.FiscalRegime, TaxOperationType.Sale, null, company.State, sale.Client?.State ?? company.State, p.Ncm, p.Cest, DateTime.UtcNow, item.Total), ct));
        }

        Invoice invoice;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            var series = model == InvoiceModel.NFeModel55 ? cfg.NfeSeries : cfg.NfceSeries;
            var counter = await db.InvoiceSeriesCounters.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Model == model && x.Series == series, ct);
            if (counter is null) { counter = new InvoiceSeriesCounter { CompanyId = companyId, Model = model, Series = series, LastNumber = 0 }; db.InvoiceSeriesCounters.Add(counter); }
            counter.LastNumber++;
            invoice = new Invoice { CompanyId = companyId, SaleId = saleId, Model = model, Environment = cfg.Environment, Series = series, Number = counter.LastNumber, Status = InvoiceStatus.Pending, Provider = provider.Name, IdempotencyKey = idempotencyKey };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        var request = new FiscalProviderRequest(companyId, invoice.Id, saleId, model, cfg.Environment, invoice.Series, invoice.Number, idempotencyKey, cfg.CertificateThumbprint!, allowContingency && cfg.AllowNfceContingency, taxes);
        FiscalProviderResult result;
        try { result = await provider.IssueAsync(request, ct); }
        catch (Exception ex) { result = new FiscalProviderResult(FiscalProviderOperationStatus.Error, RejectionCode: "PROVIDER_EXCEPTION", RejectionMessage: ex.Message); }
        Apply(invoice, result);
        if (!string.IsNullOrWhiteSpace(result.XmlContent)) StoreXml(invoice, result.XmlContent!);
        if (!string.IsNullOrWhiteSpace(result.DanfeContentBase64)) StoreDanfe(invoice, result.DanfeContentBase64!);
        invoice.Events.Add(Event(FiscalEventType.Authorization, idempotencyKey + ":authorization", result));
        await db.SaveChangesAsync(ct);
        return Map(invoice);
    }

    public async Task<FiscalInvoiceDto> QueryAsync(Guid companyId, Guid invoiceId, CancellationToken ct)
    {
        var (invoice, cfg, provider, request) = await LoadOperationAsync(companyId, invoiceId, ct);
        if (string.IsNullOrWhiteSpace(invoice.ExternalDocumentId)) throw new BusinessException("Documento ainda não possui identificador externo para consulta.");
        var result = await provider.QueryAsync(request, invoice.ExternalDocumentId, ct);
        Apply(invoice, result);
        if (!string.IsNullOrWhiteSpace(result.XmlContent)) StoreXml(invoice, result.XmlContent!);
        if (!string.IsNullOrWhiteSpace(result.DanfeContentBase64)) StoreDanfe(invoice, result.DanfeContentBase64!);
        invoice.Events.Add(Event(FiscalEventType.Query, $"query:{Guid.NewGuid():N}", result));
        await db.SaveChangesAsync(ct); return Map(invoice);
    }

    public async Task<FiscalInvoiceDto> CancelAsync(Guid companyId, Guid invoiceId, string reason, string idempotencyKey, CancellationToken ct)
    {
        idempotencyKey = RequiredIdempotencyKey(idempotencyKey);
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 15) throw new BusinessException("Motivo de cancelamento fiscal insuficiente.");
        var (invoice, cfg, provider, request) = await LoadOperationAsync(companyId, invoiceId, ct);
        if (invoice.Status != InvoiceStatus.Authorized || string.IsNullOrWhiteSpace(invoice.ExternalDocumentId)) throw new BusinessException("Apenas documento autorizado com identificador externo pode ser cancelado.");
        if (invoice.Events.Any(x => x.IdempotencyKey == idempotencyKey)) return Map(invoice);
        invoice.Status = InvoiceStatus.CancelRequested; await db.SaveChangesAsync(ct);
        var result = await provider.CancelAsync(request, invoice.ExternalDocumentId, reason.Trim(), idempotencyKey, ct);
        if (result.Status == FiscalProviderOperationStatus.Cancelled && !string.IsNullOrWhiteSpace(result.Protocol))
        {
            invoice.Status = InvoiceStatus.Cancelled; invoice.CancelledAt = DateTime.UtcNow; invoice.CancelReason = reason.Trim(); invoice.Protocol = result.Protocol;
        }
        else if (result.Status == FiscalProviderOperationStatus.Cancelled)
        {
            invoice.Status = InvoiceStatus.Error; invoice.RejectionCode = "INVALID_PROVIDER_RESPONSE"; invoice.RejectionMessage = "O provedor informou cancelamento sem protocolo verificável.";
        }
        else Apply(invoice, result);
        invoice.Events.Add(Event(FiscalEventType.Cancellation, idempotencyKey, result)); await db.SaveChangesAsync(ct); return Map(invoice);
    }

    public async Task<FiscalInvoiceDto> CorrectionLetterAsync(Guid companyId, Guid invoiceId, string correction, string idempotencyKey, CancellationToken ct)
    {
        idempotencyKey = RequiredIdempotencyKey(idempotencyKey);
        if (string.IsNullOrWhiteSpace(correction) || correction.Trim().Length < 15) throw new BusinessException("Texto da carta de correção insuficiente.");
        var (invoice, cfg, provider, request) = await LoadOperationAsync(companyId, invoiceId, ct);
        if (invoice.Model != InvoiceModel.NFeModel55 || invoice.Status != InvoiceStatus.Authorized || string.IsNullOrWhiteSpace(invoice.ExternalDocumentId)) throw new BusinessException("Carta de correção exige NF-e autorizada e suporte do provedor.");
        if (invoice.Events.Any(x => x.IdempotencyKey == idempotencyKey)) return Map(invoice);
        var result = await provider.CorrectionLetterAsync(request, invoice.ExternalDocumentId, correction.Trim(), idempotencyKey, ct);
        invoice.Events.Add(Event(FiscalEventType.CorrectionLetter, idempotencyKey, result)); await db.SaveChangesAsync(ct); return Map(invoice);
    }

    public async Task<IReadOnlyList<FiscalNumberVoidDto>> GetNumberVoidsAsync(Guid companyId, CancellationToken ct) =>
        await db.FiscalNumberVoids.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Take(1000)
            .Select(x => new FiscalNumberVoidDto(x.Id, x.Model, x.Environment, x.Series, x.FromNumber, x.ToNumber, x.Reason, x.Status, x.Provider, x.Protocol, x.RejectionCode, x.RejectionMessage, x.CreatedAt, x.CompletedAt))
            .ToListAsync(ct);

    public async Task<FiscalNumberVoidDto> VoidNumbersAsync(Guid companyId, InvoiceModel model, int series, int fromNumber, int toNumber, string reason, string idempotencyKey, CancellationToken ct)
    {
        if (series <= 0 || fromNumber <= 0 || toNumber < fromNumber) throw new BusinessException("Série e faixa numérica são inválidas.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 15) throw new BusinessException("Informe uma justificativa fiscal com ao menos 15 caracteres.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new BusinessException("Chave de idempotência obrigatória.");
        idempotencyKey = RequiredIdempotencyKey(idempotencyKey);

        var prior = await db.FiscalNumberVoids.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IdempotencyKey == idempotencyKey, ct);
        if (prior is not null)
        {
            if (prior.Model != model || prior.Series != series || prior.FromNumber != fromNumber || prior.ToNumber != toNumber || !string.Equals(prior.Reason, reason.Trim(), StringComparison.Ordinal))
                throw new BusinessException("A chave de idempotência já foi utilizada para outra inutilização.");
            return Map(prior);
        }

        var overlap = await db.FiscalNumberVoids.AnyAsync(x => x.CompanyId == companyId && x.Model == model && x.Series == series && x.IsActive &&
            x.Status != FiscalNumberVoidStatus.Rejected && x.Status != FiscalNumberVoidStatus.Error &&
            x.FromNumber <= toNumber && x.ToNumber >= fromNumber, ct);
        if (overlap) throw new BusinessException("A faixa solicitada se sobrepõe a outra inutilização já registrada ou em processamento.");

        var cfg = await db.FiscalConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Enabled && x.IsActive, ct)
            ?? throw new BusinessException("Configuração fiscal habilitada não encontrada.");
        if (string.IsNullOrWhiteSpace(cfg.CertificateThumbprint)) throw new BusinessException("Certificado fiscal não configurado.");
        var provider = providers.FirstOrDefault(x => string.Equals(x.Name, cfg.Provider, StringComparison.OrdinalIgnoreCase))
            ?? providers.First(x => x.Name == "NotConfigured");

        var record = new FiscalNumberVoid
        {
            CompanyId = companyId,
            Model = model,
            Environment = cfg.Environment,
            Series = series,
            FromNumber = fromNumber,
            ToNumber = toNumber,
            Reason = reason.Trim(),
            Provider = provider.Name,
            IdempotencyKey = idempotencyKey,
            Status = FiscalNumberVoidStatus.Processing
        };
        db.FiscalNumberVoids.Add(record);
        await db.SaveChangesAsync(ct);

        var request = new FiscalProviderRequest(companyId, Guid.Empty, Guid.Empty, model, cfg.Environment, series, fromNumber, idempotencyKey, cfg.CertificateThumbprint!, false, Array.Empty<TaxCalculationResult>());
        FiscalProviderResult result;
        try { result = await provider.VoidNumberAsync(request, fromNumber, toNumber, reason.Trim(), idempotencyKey, ct); }
        catch (Exception ex) { result = new FiscalProviderResult(FiscalProviderOperationStatus.Error, RejectionCode: "PROVIDER_EXCEPTION", RejectionMessage: ex.Message); }

        record.ExternalEventId = result.ExternalEventId;
        record.Protocol = result.Protocol;
        record.RejectionCode = result.RejectionCode;
        record.RejectionMessage = result.RejectionMessage;
        record.Status = result.Status switch
        {
            FiscalProviderOperationStatus.Authorized when !string.IsNullOrWhiteSpace(result.Protocol) => FiscalNumberVoidStatus.Voided,
            FiscalProviderOperationStatus.Accepted or FiscalProviderOperationStatus.Pending => FiscalNumberVoidStatus.Processing,
            FiscalProviderOperationStatus.Rejected => FiscalNumberVoidStatus.Rejected,
            FiscalProviderOperationStatus.Unsupported => FiscalNumberVoidStatus.ProviderNotConfigured,
            _ => FiscalNumberVoidStatus.Error
        };
        if (result.Status == FiscalProviderOperationStatus.Authorized && string.IsNullOrWhiteSpace(result.Protocol))
        {
            record.Status = FiscalNumberVoidStatus.Error;
            record.RejectionCode = "INVALID_PROVIDER_RESPONSE";
            record.RejectionMessage = "O provedor informou inutilização sem protocolo verificável.";
        }
        if (record.Status == FiscalNumberVoidStatus.Voided) record.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(result.XmlContent)) StoreVoidXml(record, result.XmlContent!);
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(record);
    }

    public async Task<IReadOnlyList<TaxGuideDto>> GetTaxGuidesAsync(Guid companyId, CancellationToken ct) => await db.TaxGuides.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.DueDate).Select(x => new TaxGuideDto(x.Id, x.TaxType, x.ReferenceYear, x.ReferenceMonth, x.TotalAmount, x.DueDate, x.Status, x.RevenueCode, x.DigitableLine, x.DocumentPath, x.ProofPath, x.AccountPayableId)).ToListAsync(ct);

    public async Task<TaxGuideDto> CreateTaxGuideAsync(Guid companyId, string taxType, int referenceYear, int referenceMonth, DateTime dueDate, decimal principalAmount, string? revenueCode, string? digitableLine, string? documentPath, Guid? accountPayableId, string? originDescription, CancellationToken ct)
    {
        if (principalAmount <= 0 || referenceMonth is < 1 or > 12) throw new BusinessException("Dados da guia são inválidos.");
        if (accountPayableId.HasValue && !await db.AccountsPayable.AnyAsync(x => x.Id == accountPayableId && x.CompanyId == companyId && x.IsActive, ct)) throw new BusinessException("Conta a pagar vinculada não encontrada.");
        var g = new TaxGuide { CompanyId = companyId, AccountPayableId = accountPayableId, TaxType = taxType.Trim(), ReferenceYear = referenceYear, ReferenceMonth = referenceMonth, DueDate = dueDate.ToUniversalTime(), PrincipalAmount = principalAmount, TotalAmount = principalAmount, RevenueCode = revenueCode?.Trim(), DigitableLine = digitableLine?.Trim(), DocumentPath = documentPath, OriginDescription = originDescription?.Trim() };
        db.TaxGuides.Add(g); await db.SaveChangesAsync(ct); return Map(g);
    }
    public async Task<TaxGuideDto> MarkTaxGuidePaidAsync(Guid companyId, Guid guideId, string proofPath, CancellationToken ct)
    {
        var g = await db.TaxGuides.FirstOrDefaultAsync(x => x.Id == guideId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Guia não encontrada.");
        if (string.IsNullOrWhiteSpace(proofPath)) throw new BusinessException("Comprovante é obrigatório para marcar a guia como paga.");
        g.ProofPath = proofPath.Trim(); g.Status = TaxGuideStatus.Paid; g.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Map(g);
    }

    private async Task<(Invoice Invoice, FiscalConfiguration Cfg, IFiscalProvider Provider, FiscalProviderRequest Request)> LoadOperationAsync(Guid companyId, Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == invoiceId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Documento fiscal não encontrado.");
        var cfg = await db.FiscalConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Enabled && x.IsActive, ct) ?? throw new BusinessException("Configuração fiscal não encontrada.");
        var provider = providers.FirstOrDefault(x => string.Equals(x.Name, invoice.Provider, StringComparison.OrdinalIgnoreCase)) ?? providers.First(x => x.Name == "NotConfigured");
        if (string.IsNullOrWhiteSpace(cfg.CertificateThumbprint)) throw new BusinessException("Certificado fiscal não configurado.");
        var request = new FiscalProviderRequest(companyId, invoice.Id, invoice.SaleId ?? Guid.Empty, invoice.Model, invoice.Environment, invoice.Series, invoice.Number, invoice.IdempotencyKey, cfg.CertificateThumbprint!, cfg.AllowNfceContingency, Array.Empty<TaxCalculationResult>());
        return (invoice, cfg, provider, request);
    }
    private static void Apply(Invoice i, FiscalProviderResult r)
    {
        i.ExternalDocumentId = r.ExternalDocumentId ?? i.ExternalDocumentId;
        i.AccessKey = r.AccessKey ?? i.AccessKey;
        i.Protocol = r.Protocol ?? i.Protocol;
        i.RejectionCode = r.RejectionCode;
        i.RejectionMessage = r.RejectionMessage;
        i.IsContingency = r.IsContingency;

        var hasAuthorizationEvidence = IsValidAccessKey(i.AccessKey) && !string.IsNullOrWhiteSpace(i.Protocol);
        i.Status = r.Status switch
        {
            FiscalProviderOperationStatus.Authorized when hasAuthorizationEvidence => InvoiceStatus.Authorized,
            FiscalProviderOperationStatus.Authorized => InvoiceStatus.Error,
            FiscalProviderOperationStatus.Accepted or FiscalProviderOperationStatus.Pending => InvoiceStatus.Processing,
            FiscalProviderOperationStatus.Rejected => InvoiceStatus.Rejected,
            FiscalProviderOperationStatus.Cancelled => InvoiceStatus.Cancelled,
            FiscalProviderOperationStatus.Unsupported => InvoiceStatus.ProviderNotConfigured,
            FiscalProviderOperationStatus.Error => InvoiceStatus.Error,
            _ => InvoiceStatus.Error
        };

        if (r.Status == FiscalProviderOperationStatus.Authorized && !hasAuthorizationEvidence)
        {
            i.RejectionCode = "INVALID_PROVIDER_RESPONSE";
            i.RejectionMessage = "O provedor informou autorização sem chave de acesso de 44 dígitos e protocolo verificável.";
        }
        if (i.Status == InvoiceStatus.Authorized) i.AuthorizedAt = DateTime.UtcNow;
        if (r.IsContingency)
        {
            i.Status = InvoiceStatus.Contingency;
            i.ContingencyStartedAt ??= DateTime.UtcNow;
            i.AuthorizedAt = null;
        }
        i.UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidAccessKey(string? key) => key is { Length: 44 } && key.All(char.IsDigit);

    private static InvoiceEvent Event(FiscalEventType type, string key, FiscalProviderResult r) => new() { Type = type, EventType = type.ToString(), IdempotencyKey = key, Status = r.Status.ToString(), Protocol = r.Protocol, ExternalEventId = r.ExternalEventId, RejectionCode = r.RejectionCode, RejectionMessage = r.RejectionMessage };
    private void StoreXml(Invoice invoice, string xml)
    {
        var root = FiscalRoot(invoice.CompanyId, invoice.CreatedAt);
        var path = Path.Combine(root, $"{invoice.Model}-{invoice.Series}-{invoice.Number}.xml"); File.WriteAllText(path, xml, Encoding.UTF8); invoice.XmlPath = path; invoice.XmlSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml)));
    }
    private void StoreDanfe(Invoice invoice, string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length == 0) return;
            var root = FiscalRoot(invoice.CompanyId, invoice.CreatedAt);
            var path = Path.Combine(root, $"{invoice.Model}-{invoice.Series}-{invoice.Number}-danfe.pdf");
            File.WriteAllBytes(path, bytes);
            invoice.DanfePath = path;
        }
        catch (FormatException)
        {
            invoice.Status = InvoiceStatus.Error;
            invoice.RejectionCode = "INVALID_DANFE_PAYLOAD";
            invoice.RejectionMessage = "O provedor retornou DANFE em Base64 inválido.";
        }
    }

    private void StoreVoidXml(FiscalNumberVoid record, string xml)
    {
        var root = FiscalRoot(record.CompanyId, record.CreatedAt);
        var path = Path.Combine(root, $"inutilizacao-{record.Model}-{record.Series}-{record.FromNumber}-{record.ToNumber}.xml");
        File.WriteAllText(path, xml, Encoding.UTF8);
        record.XmlPath = path;
    }

    private string FiscalRoot(Guid companyId, DateTime createdAt)
    {
        var configuredRoot = configuration["Storage:FiscalRoot"];
        var baseRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NinaERP", "fiscal")
            : Path.GetFullPath(configuredRoot);
        var root = Path.Combine(baseRoot, companyId.ToString("N"), createdAt.ToString("yyyyMM"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static FiscalConfigurationDto Map(FiscalConfiguration x) => new(x.Id,x.Environment,x.Provider,x.CertificateThumbprint,x.ProviderSecretReference,x.NfceCscId,x.NfceCscSecretReference,x.NfeSeries,x.NfceSeries,x.AllowNfceContingency,x.Enabled);
    private static FiscalInvoiceDto Map(Invoice x) => new(x.Id, x.SaleId, x.Model, x.Environment, x.Series, x.Number, x.AccessKey, x.Status, x.Provider, x.Protocol, x.RejectionCode, x.RejectionMessage, x.IsContingency, x.AuthorizedAt, x.CreatedAt);
    private static FiscalNumberVoidDto Map(FiscalNumberVoid x) => new(x.Id, x.Model, x.Environment, x.Series, x.FromNumber, x.ToNumber, x.Reason, x.Status, x.Provider, x.Protocol, x.RejectionCode, x.RejectionMessage, x.CreatedAt, x.CompletedAt);
    private static TaxGuideDto Map(TaxGuide x) => new(x.Id, x.TaxType, x.ReferenceYear, x.ReferenceMonth, x.TotalAmount, x.DueDate, x.Status, x.RevenueCode, x.DigitableLine, x.DocumentPath, x.ProofPath, x.AccountPayableId);
    private static string RequiredIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new BusinessException("Chave de idempotência fiscal obrigatória.");
        var normalized = key.Trim();
        if (normalized.Length > 120) throw new BusinessException("Chave de idempotência fiscal excede 120 caracteres.");
        return normalized;
    }

}
