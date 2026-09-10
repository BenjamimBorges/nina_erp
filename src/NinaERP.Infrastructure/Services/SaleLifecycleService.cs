using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class SaleLifecycleService(AppDbContext db) : ISaleLifecycleService
{
    public async Task SuspendAsync(Guid companyId, Guid userId, Guid saleId, CancellationToken ct)
    {
        var sale = await FindAsync(companyId, saleId, ct);
        if (sale.UserId != userId) throw new BusinessException("Somente o operador responsável pode suspender esta venda.");
        if (sale.Status != SaleStatus.Open) throw new BusinessException("Somente venda aberta pode ser suspensa.");
        sale.Status = SaleStatus.Suspended;
        sale.SuspendedAt = DateTime.UtcNow;
        sale.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ResumeAsync(Guid companyId, Guid userId, Guid saleId, CancellationToken ct)
    {
        var sale = await FindAsync(companyId, saleId, ct);
        if (sale.UserId != userId) throw new BusinessException("Somente o operador responsável pode recuperar esta venda.");
        if (sale.Status != SaleStatus.Suspended) throw new BusinessException("Somente venda suspensa pode ser recuperada.");
        sale.Status = SaleStatus.Open;
        sale.SuspendedAt = null;
        sale.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid companyId, Guid userId, Guid saleId, string reason, string idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new BusinessException("Informe o motivo do cancelamento.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new BusinessException("Chave de idempotência obrigatória.");
        idempotencyKey = idempotencyKey.Trim();
        var requestHash = $"{saleId:N}:{reason.Trim()}";
        var prior = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == "SaleCancel" && x.Key == idempotencyKey, ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal)) throw new BusinessException("A chave de idempotência já foi utilizada para outro cancelamento.");
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var sale = await db.Sales
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.PaymentTransactions)
            .Include(x => x.Invoices)
            .FirstOrDefaultAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Venda não encontrada.");

        if (sale.Status == SaleStatus.Cancelled) return;
        if (sale.Invoices.Any(x => x.IsActive && x.Status is InvoiceStatus.Authorized or InvoiceStatus.Processing or InvoiceStatus.Contingency or InvoiceStatus.CancelRequested))
            throw new BusinessException("A venda possui documento fiscal ativo. Conclua o cancelamento fiscal antes do cancelamento operacional.");

        var captured = sale.PaymentTransactions
            .Where(x => x.IsActive && x.Status is PaymentTransactionStatus.Captured or PaymentTransactionStatus.PartiallyRefunded)
            .ToList();
        var cashCaptured = captured.Where(IsManualCash).ToList();
        var externalCaptured = captured.Where(x => !IsManualCash(x) && x.Amount > x.RefundedAmount).ToList();

        // Dinheiro físico pode ser estornado localmente. Pagamentos externos jamais são marcados
        // como estornados antes da resposta do adquirente/provedor.
        foreach (var payment in cashCaptured)
            RefundManualCash(companyId, userId, sale, payment);

        foreach (var payment in sale.PaymentTransactions.Where(x => x.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Processing or PaymentTransactionStatus.Authorized))
            payment.Status = PaymentTransactionStatus.Cancelled;

        var paymentIds = sale.PaymentTransactions.Select(x => x.Id).ToArray();
        var startMessages = await db.OutboxMessages.Where(x => x.CompanyId == companyId && x.Type == "PaymentStartRequested" &&
            (x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Processing) && paymentIds.Contains(x.AggregateId)).ToListAsync(ct);
        foreach (var message in startMessages)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.LastError = "Venda cancelada antes da confirmação do pagamento externo.";
        }

        if (externalCaptured.Count > 0)
        {
            foreach (var payment in externalCaptured)
            {
                if (string.IsNullOrWhiteSpace(payment.ExternalTransactionId))
                {
                    payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                    payment.FailureCode = "EXTERNAL_ID_MISSING";
                    payment.FailureMessage = "Pagamento capturado sem identificador externo; estorno exige conciliação manual com o provedor.";
                    continue;
                }

                payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                db.OutboxMessages.Add(new OutboxMessage
                {
                    CompanyId = companyId,
                    Type = "PaymentRefundRequested",
                    AggregateType = nameof(PaymentTransaction),
                    AggregateId = payment.Id,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        payment.Id,
                        Amount = payment.Amount - payment.RefundedAmount,
                        payment.Provider,
                        payment.ExternalTransactionId,
                        IdempotencyKey = $"{idempotencyKey}:{payment.Id:N}"
                    })
                });
            }

            sale.Status = SaleStatus.CancellationPending;
            sale.CancellationReason = reason.Trim();
            sale.UpdatedAt = DateTime.UtcNow;
            db.IdempotencyRecords.Add(NewCancelIdempotency(companyId, idempotencyKey, requestHash, sale.Id));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        await ReverseInventoryForCancellationAsync(sale, userId, reason.Trim(), ct);
        sale.Status = SaleStatus.Cancelled;
        sale.CancelledAt = DateTime.UtcNow;
        sale.CancellationReason = reason.Trim();
        sale.UpdatedAt = DateTime.UtcNow;
        db.IdempotencyRecords.Add(NewCancelIdempotency(companyId, idempotencyKey, requestHash, sale.Id));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ReverseInventoryForCancellationAsync(Sale sale, Guid userId, string reason, CancellationToken ct)
    {
        var reservations = await db.InventoryReservations.Where(x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.IsActive).ToListAsync(ct);
        if (reservations.Count > 0)
        {
            foreach (var reservation in reservations)
            {
                var balance = await db.InventoryBalances.FirstOrDefaultAsync(x => x.CompanyId == sale.CompanyId && x.WarehouseId == reservation.WarehouseId && x.ProductId == reservation.ProductId && x.IsActive, ct)
                    ?? throw new BusinessException("Saldo de estoque da reserva não foi encontrado durante o cancelamento.");
                var product = await db.Products.FirstAsync(x => x.Id == reservation.ProductId && x.CompanyId == sale.CompanyId && x.IsActive, ct);

                if (reservation.ActiveQuantity > 0)
                {
                    if (balance.Reserved < reservation.ActiveQuantity) throw new BusinessException("Saldo reservado inconsistente; cancelamento exige conciliação de estoque.");
                    balance.Reserved -= reservation.ActiveQuantity;
                    reservation.ReleasedQuantity += reservation.ActiveQuantity;
                    reservation.ReleasedAt = DateTime.UtcNow;
                    reservation.ReleaseReason = reason;
                }

                if (reservation.ConsumedQuantity > 0)
                {
                    var restore = reservation.ConsumedQuantity;
                    balance.OnHand += restore;
                    product.StockQty += restore;
                    product.UpdatedAt = DateTime.UtcNow;
                    var key = $"cancel:{sale.Id:N}:product:{reservation.ProductId:N}";
                    if (!await db.StockMovements.AnyAsync(x => x.CompanyId == sale.CompanyId && x.IdempotencyKey == key, ct))
                    {
                        db.StockMovements.Add(new StockMovement
                        {
                            CompanyId = sale.CompanyId,
                            ProductId = reservation.ProductId,
                            WarehouseId = reservation.WarehouseId,
                            UserId = userId,
                            Type = StockMovementType.Entry,
                            Qty = restore,
                            CostUnit = balance.AverageCost,
                            OriginType = "SaleCancellation",
                            OriginId = sale.Id,
                            IdempotencyKey = key,
                            Notes = reason
                        });
                    }
                }
            }
            return;
        }

        // Compatibilidade com vendas criadas antes da introdução de InventoryReservation.
        if (sale.CompletedAt is null) return;
        var warehouseId = sale.WarehouseId ?? await db.Warehouses.Where(x => x.CompanyId == sale.CompanyId && x.IsDefault && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        foreach (var group in sale.Items.GroupBy(x => x.ProductId))
        {
            var qty = group.Sum(x => x.Qty);
            var product = group.First().Product!;
            product.StockQty += qty;
            product.UpdatedAt = DateTime.UtcNow;
            if (warehouseId.HasValue)
            {
                var balance = await db.InventoryBalances.FirstOrDefaultAsync(x => x.CompanyId == sale.CompanyId && x.WarehouseId == warehouseId && x.ProductId == group.Key, ct);
                if (balance is null)
                {
                    balance = new InventoryBalance { CompanyId = sale.CompanyId, WarehouseId = warehouseId.Value, ProductId = group.Key, OnHand = 0, AverageCost = product.CostAverage };
                    db.InventoryBalances.Add(balance);
                }
                balance.OnHand += qty;
            }
            db.StockMovements.Add(new StockMovement { CompanyId = sale.CompanyId, ProductId = group.Key, WarehouseId = warehouseId, UserId = userId, Type = StockMovementType.Entry, Qty = qty, CostUnit = product.CostAverage, OriginType = "SaleCancellation", OriginId = sale.Id, IdempotencyKey = $"cancel:{sale.Id:N}:product:{group.Key:N}", Notes = reason });
        }
    }

    private void RefundManualCash(Guid companyId, Guid userId, Sale sale, PaymentTransaction payment)
    {
        var amount = payment.Amount - payment.RefundedAmount;
        if (amount <= 0) return;
        payment.RefundedAmount = payment.Amount;
        payment.RefundedAt = DateTime.UtcNow;
        payment.Status = PaymentTransactionStatus.Refunded;
        if (payment.CashSessionId.HasValue)
        {
            db.CashMovements.Add(new CashMovement
            {
                CompanyId = companyId,
                CashSessionId = payment.CashSessionId.Value,
                UserId = userId,
                SaleId = sale.Id,
                PaymentTransactionId = payment.Id,
                Type = CashMovementType.Refund,
                PaymentMethod = PaymentMethod.Cash,
                Amount = amount,
                Description = "Cancelamento de venda em dinheiro"
            });
        }
    }

    private static bool IsManualCash(PaymentTransaction p) => p.Method == PaymentMethod.Cash && string.Equals(p.Provider, "manual", StringComparison.OrdinalIgnoreCase);

    private static IdempotencyRecord NewCancelIdempotency(Guid companyId, string key, string hash, Guid saleId) => new()
    {
        CompanyId = companyId,
        Scope = "SaleCancel",
        Key = key,
        RequestHash = hash,
        ResourceId = saleId,
        ExpiresAt = DateTime.UtcNow.AddYears(5)
    };

    private async Task<Sale> FindAsync(Guid companyId, Guid saleId, CancellationToken ct) =>
        await db.Sales.FirstOrDefaultAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct)
        ?? throw new BusinessException("Venda não encontrada.");
}
