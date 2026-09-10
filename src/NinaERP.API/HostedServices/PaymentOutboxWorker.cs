using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.API.HostedServices;

public sealed class PaymentOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PaymentOutboxWorker> logger) : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Outbox:PollingSeconds", 5)));
    private readonly int _batchSize = Math.Clamp(configuration.GetValue("Outbox:BatchSize", 20), 1, 100);
    private readonly int _maxAttempts = Math.Clamp(configuration.GetValue("Outbox:MaxAttempts", 8), 1, 50);
    private readonly TimeSpan _processingTimeout = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Outbox:ProcessingTimeoutMinutes", 10), 1, 120));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                    await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha não tratada no worker da outbox de pagamentos.");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await RecoverStaleProcessingAsync(db, ct);

        var ids = await db.OutboxMessages.AsNoTracking()
            .Where(x => x.IsActive && x.Status == OutboxMessageStatus.Pending &&
                        (x.NextAttemptAt == null || x.NextAttemptAt <= DateTime.UtcNow) &&
                        (x.Type == "PaymentStartRequested" || x.Type == "PaymentRefundRequested"))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(_batchSize)
            .ToListAsync(ct);

        foreach (var id in ids)
            await ProcessOneAsync(id, ct);

        return ids.Count;
    }

    private async Task RecoverStaleProcessingAsync(AppDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.Subtract(_processingTimeout);
        var stale = await db.OutboxMessages
            .Where(x => x.IsActive && x.Status == OutboxMessageStatus.Processing &&
                        x.UpdatedAt <= cutoff &&
                        (x.Type == "PaymentStartRequested" || x.Type == "PaymentRefundRequested"))
            .OrderBy(x => x.UpdatedAt)
            .Take(_batchSize)
            .ToListAsync(ct);

        if (stale.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var message in stale)
        {
            if (message.Attempts >= _maxAttempts)
            {
                message.Status = OutboxMessageStatus.Failed;
                message.LastError = "PROCESSING_TIMEOUT: mensagem permaneceu em processamento além do limite e esgotou as tentativas.";
                message.NextAttemptAt = null;

                var payment = await db.PaymentTransactions.FirstOrDefaultAsync(
                    x => x.Id == message.AggregateId && x.CompanyId == message.CompanyId && x.IsActive, ct);
                if (payment is not null && payment.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Processing or PaymentTransactionStatus.Authorized)
                {
                    payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                    payment.FailureCode = "OUTBOX_PROCESSING_TIMEOUT";
                    payment.FailureMessage = "O processamento externo foi interrompido e exige conciliação antes de nova tentativa manual.";
                }
            }
            else
            {
                message.Status = OutboxMessageStatus.Pending;
                message.NextAttemptAt = now;
                message.LastError = "PROCESSING_TIMEOUT: mensagem recuperada automaticamente após interrupção do worker.";
            }

            message.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        logger.LogWarning("Recuperadas {Count} mensagens da outbox que estavam presas em Processing.", stale.Count);
    }

    private async Task ProcessOneAsync(Guid id, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var claimed = await db.OutboxMessages
            .Where(x => x.Id == id && x.Status == OutboxMessageStatus.Pending && x.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxMessageStatus.Processing)
                .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);

        if (claimed == 0)
            return;

        var message = await db.OutboxMessages.FirstAsync(x => x.Id == id, ct);
        var payment = await db.PaymentTransactions.FirstOrDefaultAsync(
            x => x.Id == message.AggregateId && x.CompanyId == message.CompanyId && x.IsActive, ct);

        if (payment is null)
        {
            await FailPermanentlyAsync(db, message, "PAYMENT_NOT_FOUND", "A transação associada à outbox não existe.", ct);
            return;
        }

        var adapters = scope.ServiceProvider.GetServices<IPaymentProviderAdapter>();
        var adapter = adapters.FirstOrDefault(x =>
            string.Equals(x.ProviderName, payment.Provider, StringComparison.OrdinalIgnoreCase) &&
            x.Supports(payment.Method));

        if (adapter is null)
        {
            payment.Status = PaymentTransactionStatus.ReconciliationRequired;
            payment.FailureCode = "PROVIDER_NOT_CONFIGURED";
            payment.FailureMessage = $"Nenhum adapter registrado para o provedor '{payment.Provider}' e método '{payment.Method}'.";
            await FailPermanentlyAsync(db, message, payment.FailureCode, payment.FailureMessage, ct);
            return;
        }

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            if (message.Type == "PaymentStartRequested")
            {
                var result = await adapter.StartAsync(
                    message.CompanyId,
                    payment.SaleId,
                    payment.Amount,
                    payment.IdempotencyKey,
                    ct);
                await service.ConfirmExternalAsync(message.CompanyId, payment.Id, result, PaymentConfirmationSource.Provider, ct);
            }
            else
            {
                using var json = JsonDocument.Parse(message.PayloadJson);
                var root = json.RootElement;
                var amount = root.GetProperty("Amount").GetDecimal();
                var idempotencyKey = root.GetProperty("IdempotencyKey").GetString()
                    ?? throw new InvalidOperationException("Outbox de estorno sem chave de idempotência.");
                var externalId = payment.ExternalTransactionId
                    ?? throw new InvalidOperationException("Transação de estorno sem identificador externo.");

                var result = await adapter.RefundAsync(
                    message.CompanyId,
                    externalId,
                    amount,
                    idempotencyKey,
                    ct);
                await service.ConfirmRefundAsync(
                    message.CompanyId,
                    payment.Id,
                    amount,
                    idempotencyKey,
                    result,
                    PaymentConfirmationSource.Provider,
                    ct);
            }

            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAt = DateTime.UtcNow;
            message.NextAttemptAt = null;
            message.LastError = null;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha processando outbox {OutboxId} do pagamento {PaymentId}.", message.Id, payment.Id);
            message.LastError = Truncate(ex.Message, 1000);
            if (message.Attempts >= _maxAttempts)
            {
                message.Status = OutboxMessageStatus.Failed;
                payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                payment.FailureCode ??= "OUTBOX_RETRY_EXHAUSTED";
                payment.FailureMessage = "O limite de tentativas automáticas foi atingido; a transação exige conciliação.";
            }
            else
            {
                message.Status = OutboxMessageStatus.Pending;
                message.NextAttemptAt = DateTime.UtcNow.Add(Backoff(message.Attempts));
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task FailPermanentlyAsync(
        AppDbContext db,
        NinaERP.Domain.Entities.OutboxMessage message,
        string code,
        string text,
        CancellationToken ct)
    {
        message.Status = OutboxMessageStatus.Failed;
        message.LastError = Truncate($"{code}: {text}", 1000);
        message.NextAttemptAt = null;
        await db.SaveChangesAsync(ct);
    }

    private static TimeSpan Backoff(int attempts)
    {
        var seconds = Math.Min(900, Math.Pow(2, Math.Clamp(attempts, 1, 10)) * 5);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
