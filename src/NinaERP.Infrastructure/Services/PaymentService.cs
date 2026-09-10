using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class PaymentService(AppDbContext db) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentTransactionDto>> RegisterAsync(
        Guid companyId,
        Guid userId,
        Guid saleId,
        Guid cashSessionId,
        IReadOnlyList<PaymentRequestDto> payments,
        CancellationToken ct)
    {
        if (payments.Count == 0)
            throw new BusinessException("Informe ao menos uma forma de pagamento.");

        var keys = payments.Select(x => x.IdempotencyKey?.Trim()).ToArray();
        if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new BusinessException("Cada pagamento deve possuir uma chave de idempotência única.");

        var existing = await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.SaleId == saleId && keys.Contains(x.IdempotencyKey))
            .ToListAsync(ct);

        if (existing.Count == payments.Count)
        {
            ValidateRetry(existing, payments);
            return existing.OrderBy(x => x.CreatedAt).Select(Map).ToList();
        }

        if (existing.Count > 0)
            throw new BusinessException("A requisição mistura pagamentos novos e já processados. Recarregue a venda antes de tentar novamente.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var sale = await db.Sales
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.PaymentTransactions)
            .FirstOrDefaultAsync(x => x.Id == saleId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Venda não encontrada.");

        if (sale.Status is SaleStatus.Completed or SaleStatus.Cancelled or SaleStatus.CancellationPending or SaleStatus.Suspended)
            throw new BusinessException("A venda não aceita novos pagamentos neste estado.");

        var session = await db.CashSessions.FirstOrDefaultAsync(
            x => x.Id == cashSessionId && x.CompanyId == companyId && x.UserId == userId &&
                 x.Status == CashSessionStatus.Open && x.IsActive,
            ct) ?? throw new BusinessException("É necessário um caixa aberto pertencente ao operador para receber a venda.");

        await EnsureReservationsAsync(sale, ct);

        var net = decimal.Round(sale.TotalProducts - sale.TotalDiscount, 2);
        var already = sale.PaymentTransactions
            .Where(x => x.IsActive && x.Status is PaymentTransactionStatus.Captured or PaymentTransactionStatus.Authorized or PaymentTransactionStatus.Pending or PaymentTransactionStatus.Processing)
            .Sum(x => x.Amount - x.RefundedAmount);
        var requested = payments.Sum(x => x.Amount);

        if (requested <= 0 || already + requested > net)
            throw new BusinessException("Os pagamentos excedem o valor líquido da venda.");

        sale.CashSessionId = cashSessionId;
        var created = new List<PaymentTransaction>();

        foreach (var p in payments)
        {
            if (p.Amount <= 0)
                throw new BusinessException("Valor de pagamento inválido.");

            var provider = NormalizeProvider(p.Method, p.Provider);
            var immediateCash = p.Method == PaymentMethod.Cash && provider == "manual";

            var entity = new PaymentTransaction
            {
                CompanyId = companyId,
                SaleId = saleId,
                CashSessionId = cashSessionId,
                Method = p.Method,
                Amount = decimal.Round(p.Amount, 2),
                Provider = provider,
                IdempotencyKey = p.IdempotencyKey.Trim(),
                ExternalTransactionId = p.ExternalTransactionId,
                Nsu = p.Nsu,
                AuthCode = p.AuthCode,
                PixTxId = p.PixTxId,
                ConfirmationSource = immediateCash ? PaymentConfirmationSource.Manual : PaymentConfirmationSource.Provider,
                Status = immediateCash ? PaymentTransactionStatus.Captured : PaymentTransactionStatus.Pending,
                CapturedAt = immediateCash ? DateTime.UtcNow : null
            };

            db.PaymentTransactions.Add(entity);
            created.Add(entity);

            if (!immediateCash)
            {
                db.OutboxMessages.Add(new OutboxMessage
                {
                    CompanyId = companyId,
                    Type = "PaymentStartRequested",
                    AggregateType = nameof(PaymentTransaction),
                    AggregateId = entity.Id,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        entity.Id,
                        entity.SaleId,
                        entity.Provider,
                        entity.Method,
                        entity.Amount,
                        entity.IdempotencyKey
                    })
                });
            }

            if (p.Method == PaymentMethod.BankSlip)
            {
                db.AccountsReceivable.Add(new AccountReceivable
                {
                    CompanyId = companyId,
                    ClientId = sale.ClientId,
                    SaleId = sale.Id,
                    DocumentNumber = $"VEN-{sale.Id:N}",
                    Description = "Recebível originado por boleto",
                    OriginalAmount = p.Amount,
                    OpenAmount = p.Amount,
                    DueDate = p.DueDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(30),
                    OriginType = "Sale",
                    OriginId = sale.Id
                });
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await CompleteSaleIfCapturedAsync(sale, userId, ct, session);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("A venda, o estoque ou o caixa foi alterado simultaneamente. Recarregue o estado e repita com as mesmas chaves de idempotência.");
        }

        return created.Select(Map).ToList();
    }

    public async Task<PaymentTransactionDto> ConfirmExternalAsync(
        Guid companyId,
        Guid transactionId,
        PaymentProviderResult result,
        PaymentConfirmationSource source,
        CancellationToken ct)
    {
        if (source == PaymentConfirmationSource.Manual)
            throw new BusinessException("Pagamentos externos não podem ser confirmados manualmente.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var payment = await db.PaymentTransactions
            .Include(x => x.Sale)!.ThenInclude(x => x!.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Transação não encontrada.");

        if (payment.Status is PaymentTransactionStatus.Refunded or PaymentTransactionStatus.PartiallyRefunded)
            return Map(payment);

        ApplyProviderEvidence(payment, result, source);
        var hasProviderEvidence = !string.IsNullOrWhiteSpace(payment.ExternalTransactionId) || !string.IsNullOrWhiteSpace(payment.Nsu) || !string.IsNullOrWhiteSpace(payment.AuthCode) || !string.IsNullOrWhiteSpace(payment.PixTxId);
        payment.Status = result.Status switch
        {
            PaymentProviderOperationStatus.Captured when hasProviderEvidence => PaymentTransactionStatus.Captured,
            PaymentProviderOperationStatus.Authorized when hasProviderEvidence => PaymentTransactionStatus.Authorized,
            PaymentProviderOperationStatus.Captured or PaymentProviderOperationStatus.Authorized => PaymentTransactionStatus.ReconciliationRequired,
            PaymentProviderOperationStatus.Accepted or PaymentProviderOperationStatus.Pending => PaymentTransactionStatus.Processing,
            PaymentProviderOperationStatus.Rejected => PaymentTransactionStatus.Rejected,
            PaymentProviderOperationStatus.Cancelled => PaymentTransactionStatus.Cancelled,
            PaymentProviderOperationStatus.Refunded => PaymentTransactionStatus.Refunded,
            PaymentProviderOperationStatus.PartiallyRefunded => PaymentTransactionStatus.PartiallyRefunded,
            _ => PaymentTransactionStatus.ReconciliationRequired
        };
        if (!hasProviderEvidence && result.Status is PaymentProviderOperationStatus.Captured or PaymentProviderOperationStatus.Authorized)
        {
            payment.FailureCode = "INVALID_PROVIDER_RESPONSE";
            payment.FailureMessage = "O provedor informou autorização/captura sem identificador verificável.";
        }

        if (payment.Status == PaymentTransactionStatus.Authorized && payment.AuthorizedAt is null)
            payment.AuthorizedAt = DateTime.UtcNow;
        if (payment.Status == PaymentTransactionStatus.Captured && payment.CapturedAt is null)
            payment.CapturedAt = DateTime.UtcNow;

        if (payment.Sale is not null)
            await CompleteSaleIfCapturedAsync(payment.Sale, payment.Sale.UserId, ct);

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("A confirmação do pagamento concorreu com outra atualização. Recarregue a transação e reprocesse a mesma evidência do provedor.");
        }
        return Map(payment);
    }

    public async Task<PaymentTransactionDto> RefundAsync(
        Guid companyId,
        Guid userId,
        Guid transactionId,
        decimal amount,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (amount <= 0)
            throw new BusinessException("Valor de estorno inválido.");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BusinessException("Chave de idempotência é obrigatória.");

        var original = await db.PaymentTransactions.Include(x => x.Sale)
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Transação não encontrada.");

        if (original.Status is not (PaymentTransactionStatus.Captured or PaymentTransactionStatus.PartiallyRefunded))
            throw new BusinessException("A transação não está capturada.");
        if (original.RefundedAmount + amount > original.Amount)
            throw new BusinessException("O estorno excede o valor capturado.");

        var requestHash = $"{transactionId:N}:{amount:0.00}";
        var prior = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Scope == "PaymentRefund" && x.Key == idempotencyKey,
            ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal))
                throw new BusinessException("A chave de idempotência já foi utilizada para outro estorno.");
            return Map(original);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            CompanyId = companyId,
            Scope = "PaymentRefund",
            Key = idempotencyKey.Trim(),
            RequestHash = requestHash,
            ResourceId = original.Id,
            ExpiresAt = DateTime.UtcNow.AddYears(2)
        });

        if (original.Method == PaymentMethod.Cash && string.Equals(original.Provider, "manual", StringComparison.OrdinalIgnoreCase))
        {
            var refundSession = await db.CashSessions.FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId && x.Status == CashSessionStatus.Open && x.IsActive,
                ct) ?? throw new BusinessException("Abra um caixa antes de realizar um estorno em dinheiro.");

            original.RefundedAmount += amount;
            original.RefundedAt = DateTime.UtcNow;
            original.Status = original.RefundedAmount == original.Amount
                ? PaymentTransactionStatus.Refunded
                : PaymentTransactionStatus.PartiallyRefunded;

            var expectedBeforeRefund = await CalculateCashExpectedAsync(companyId, refundSession.Id, ct);
            db.CashMovements.Add(new CashMovement
            {
                CompanyId = companyId,
                CashSessionId = refundSession.Id,
                UserId = userId,
                SaleId = original.SaleId,
                PaymentTransactionId = original.Id,
                Type = CashMovementType.Refund,
                PaymentMethod = PaymentMethod.Cash,
                Amount = amount,
                Description = "Estorno financeiro em dinheiro"
            });
            refundSession.ExpectedAmount = decimal.Round(expectedBeforeRefund - amount, 2);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(original.ExternalTransactionId))
                throw new BusinessException("A transação externa não possui identificador do adquirente para solicitar estorno.");

            original.Status = PaymentTransactionStatus.ReconciliationRequired;
            db.OutboxMessages.Add(new OutboxMessage
            {
                CompanyId = companyId,
                Type = "PaymentRefundRequested",
                AggregateType = nameof(PaymentTransaction),
                AggregateId = original.Id,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    original.Id,
                    Amount = amount,
                    original.Provider,
                    original.ExternalTransactionId,
                    IdempotencyKey = idempotencyKey.Trim()
                })
            });
        }

        if (original.Sale is not null)
            original.Sale.Status = SaleStatus.ReconciliationRequired;

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("A transação ou o caixa foi alterado simultaneamente. Recarregue e repita o estorno com a mesma chave de idempotência.");
        }
        return Map(original);
    }

    public async Task<PaymentTransactionDto> ConfirmRefundAsync(
        Guid companyId,
        Guid transactionId,
        decimal amount,
        string idempotencyKey,
        PaymentProviderResult result,
        PaymentConfirmationSource source,
        CancellationToken ct)
    {
        if (source == PaymentConfirmationSource.Manual)
            throw new BusinessException("Estornos externos não podem ser confirmados manualmente.");
        if (amount <= 0 || string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BusinessException("Valor e chave de idempotência são obrigatórios para confirmar o estorno.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var payment = await db.PaymentTransactions.Include(x => x.Sale)
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Transação não encontrada.");

        var requestHash = $"{transactionId:N}:{amount:0.00}:{result.Status}";
        var prior = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Scope == "PaymentRefundConfirmation" && x.Key == idempotencyKey,
            ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal))
                throw new BusinessException("A confirmação de estorno reutilizou uma chave de idempotência incompatível.");
            return Map(payment);
        }

        if (payment.RefundedAmount + amount > payment.Amount)
            throw new BusinessException("A confirmação de estorno excede o valor da transação.");

        ApplyProviderEvidence(payment, result, source);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            CompanyId = companyId,
            Scope = "PaymentRefundConfirmation",
            Key = idempotencyKey.Trim(),
            RequestHash = requestHash,
            ResourceId = payment.Id,
            ExpiresAt = DateTime.UtcNow.AddYears(5)
        });

        switch (result.Status)
        {
            case PaymentProviderOperationStatus.Refunded:
                payment.RefundedAmount += amount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.Status = payment.RefundedAmount >= payment.Amount
                    ? PaymentTransactionStatus.Refunded
                    : PaymentTransactionStatus.PartiallyRefunded;
                break;
            case PaymentProviderOperationStatus.PartiallyRefunded:
                var confirmedPartial = result.RefundedAmount.GetValueOrDefault();
                if (confirmedPartial <= 0 || confirmedPartial > amount)
                {
                    payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                    payment.FailureCode = "INVALID_REFUND_AMOUNT";
                    payment.FailureMessage = "O provedor informou estorno parcial sem valor confirmado válido.";
                    break;
                }
                payment.RefundedAmount += confirmedPartial;
                payment.RefundedAt = DateTime.UtcNow;
                payment.Status = payment.RefundedAmount >= payment.Amount
                    ? PaymentTransactionStatus.Refunded
                    : PaymentTransactionStatus.PartiallyRefunded;
                break;
            case PaymentProviderOperationStatus.Rejected:
                payment.Status = payment.RefundedAmount > 0
                    ? PaymentTransactionStatus.PartiallyRefunded
                    : PaymentTransactionStatus.Captured;
                break;
            default:
                payment.Status = PaymentTransactionStatus.ReconciliationRequired;
                break;
        }

        if (payment.Sale is not null)
        {
            if (payment.Sale.Status == SaleStatus.CancellationPending && payment.Status is PaymentTransactionStatus.Refunded or PaymentTransactionStatus.PartiallyRefunded)
                await TryFinalizePendingCancellationAsync(payment.Sale, ct);
            else if (payment.Sale.Status != SaleStatus.CancellationPending)
                payment.Sale.Status = SaleStatus.ReconciliationRequired;
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("A confirmação do estorno concorreu com outra atualização. Recarregue a transação e reprocesse a mesma evidência do provedor.");
        }
        return Map(payment);
    }

    public async Task<IReadOnlyList<PaymentTransactionDto>> GetSaleTransactionsAsync(Guid companyId, Guid saleId, CancellationToken ct) =>
        await db.PaymentTransactions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.SaleId == saleId && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PaymentTransactionDto(
                x.Id, x.SaleId, x.Method, x.Amount, x.RefundedAmount, x.Status, x.ConfirmationSource,
                x.Provider, x.IdempotencyKey, x.ExternalTransactionId, x.FailureCode, x.FailureMessage,
                x.CreatedAt, x.CapturedAt, x.RefundedAt))
            .ToListAsync(ct);

    private async Task EnsureReservationsAsync(Sale sale, CancellationToken ct)
    {
        var warehouse = sale.WarehouseId.HasValue
            ? await db.Warehouses.FirstOrDefaultAsync(x => x.Id == sale.WarehouseId.Value && x.CompanyId == sale.CompanyId && x.IsActive, ct)
            : await db.Warehouses.FirstOrDefaultAsync(x => x.CompanyId == sale.CompanyId && x.IsDefault && x.IsActive, ct);

        if (warehouse is null)
            throw new BusinessException("Configure um depósito padrão ou informe o depósito da venda antes de receber pagamentos.");

        sale.WarehouseId = warehouse.Id;

        foreach (var group in sale.Items.GroupBy(x => x.ProductId))
        {
            var required = group.Sum(x => x.Qty);
            var reservation = await db.InventoryReservations.FirstOrDefaultAsync(
                x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.WarehouseId == warehouse.Id && x.ProductId == group.Key && x.IsActive,
                ct);

            if (reservation is not null)
            {
                if (reservation.ActiveQuantity >= required)
                    continue;
                throw new BusinessException("A reserva existente da venda não corresponde mais ao carrinho. Cancele e recrie a venda para evitar divergência de estoque.");
            }

            var balance = await db.InventoryBalances.FirstOrDefaultAsync(
                x => x.CompanyId == sale.CompanyId && x.WarehouseId == warehouse.Id && x.ProductId == group.Key && x.IsActive,
                ct) ?? throw new BusinessException($"O produto '{group.First().Product?.Name ?? group.Key.ToString()}' não possui saldo no depósito selecionado.");

            if (balance.Available < required)
                throw new BusinessException($"Estoque disponível insuficiente para '{group.First().Product?.Name ?? group.Key.ToString()}'.");

            balance.Reserved += required;
            db.InventoryReservations.Add(new InventoryReservation
            {
                CompanyId = sale.CompanyId,
                SaleId = sale.Id,
                WarehouseId = warehouse.Id,
                ProductId = group.Key,
                Quantity = required,
                ReservedAt = DateTime.UtcNow
            });
        }
    }

    private async Task CompleteSaleIfCapturedAsync(Sale sale, Guid userId, CancellationToken ct, CashSession? knownSession = null)
    {
        await EnsureCashMovementsAsync(sale, userId, ct, knownSession);

        var net = decimal.Round(sale.TotalProducts - sale.TotalDiscount, 2);
        var captured = await db.PaymentTransactions
            .Where(x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.Status == PaymentTransactionStatus.Captured && x.IsActive)
            .SumAsync(x => x.Amount - x.RefundedAmount, ct);

        sale.TotalPaid = captured;
        if (captured < net)
        {
            sale.Status = SaleStatus.PaymentPending;
            return;
        }

        if (captured > net)
            throw new BusinessException("Os pagamentos capturados excedem o valor líquido da venda e exigem conciliação.");
        if (sale.Status == SaleStatus.Completed)
            return;
        if (sale.WarehouseId is null)
            throw new BusinessException("A venda não possui depósito associado.");

        foreach (var group in sale.Items.GroupBy(x => x.ProductId))
        {
            var quantity = group.Sum(x => x.Qty);
            var product = group.First().Product ?? await db.Products.FirstAsync(
                x => x.Id == group.Key && x.CompanyId == sale.CompanyId && x.IsActive, ct);
            var balance = await db.InventoryBalances.FirstOrDefaultAsync(
                x => x.CompanyId == sale.CompanyId && x.WarehouseId == sale.WarehouseId.Value && x.ProductId == group.Key && x.IsActive,
                ct) ?? throw new BusinessException($"Saldo de estoque ausente para '{product.Name}'.");
            var reservation = await db.InventoryReservations.FirstOrDefaultAsync(
                x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.WarehouseId == sale.WarehouseId.Value && x.ProductId == group.Key && x.IsActive,
                ct) ?? throw new BusinessException($"Reserva de estoque ausente para '{product.Name}'.");

            if (reservation.ConsumedQuantity >= quantity)
                continue;
            if (reservation.ActiveQuantity < quantity)
                throw new BusinessException($"Reserva insuficiente para concluir a venda do produto '{product.Name}'.");
            if (balance.OnHand < quantity || balance.Reserved < quantity)
                throw new BusinessException($"Saldo reservado inconsistente para '{product.Name}'.");

            balance.Reserved -= quantity;
            balance.OnHand -= quantity;
            reservation.ConsumedQuantity += quantity;
            product.StockQty -= quantity;

            var movementKey = $"sale:{sale.Id:N}:product:{group.Key:N}";
            if (!await db.StockMovements.AnyAsync(x => x.CompanyId == sale.CompanyId && x.IdempotencyKey == movementKey, ct))
            {
                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = sale.CompanyId,
                    ProductId = group.Key,
                    WarehouseId = sale.WarehouseId,
                    UserId = userId,
                    Type = StockMovementType.Exit,
                    Qty = quantity,
                    CostUnit = balance.AverageCost,
                    OriginType = "Sale",
                    OriginId = sale.Id,
                    IdempotencyKey = movementKey,
                    Notes = "Baixa automática após pagamento integralmente capturado"
                });
            }
        }

        sale.Status = SaleStatus.Completed;
        sale.CompletedAt = DateTime.UtcNow;
        sale.UpdatedAt = DateTime.UtcNow;
    }

    private async Task EnsureCashMovementsAsync(Sale sale, Guid userId, CancellationToken ct, CashSession? knownSession = null)
    {
        var capturedCash = await db.PaymentTransactions
            .Where(x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.Method == PaymentMethod.Cash &&
                        x.Status == PaymentTransactionStatus.Captured && x.CashSessionId != null && x.IsActive)
            .ToListAsync(ct);

        var sessions = new Dictionary<Guid, (CashSession Session, decimal Expected)>();
        foreach (var payment in capturedCash)
        {
            if (await db.CashMovements.AnyAsync(
                    x => x.CompanyId == sale.CompanyId && x.PaymentTransactionId == payment.Id && x.Type == CashMovementType.SaleReceipt && x.IsActive,
                    ct))
                continue;

            var sessionId = payment.CashSessionId!.Value;
            if (!sessions.TryGetValue(sessionId, out var state))
            {
                var session = knownSession is not null && knownSession.Id == sessionId
                    ? knownSession
                    : await db.CashSessions.FirstOrDefaultAsync(
                        x => x.Id == sessionId && x.CompanyId == sale.CompanyId && x.Status == CashSessionStatus.Open && x.IsActive,
                        ct);
                if (session is null || session.Status != CashSessionStatus.Open)
                    throw new BusinessException("O caixa associado ao recebimento em dinheiro não está mais aberto; a venda requer conciliação.");

                state = (session, await CalculateCashExpectedAsync(sale.CompanyId, sessionId, ct));
            }

            var receipt = decimal.Round(payment.Amount - payment.RefundedAmount, 2);
            if (receipt <= 0)
                continue;

            db.CashMovements.Add(new CashMovement
            {
                CompanyId = sale.CompanyId,
                CashSessionId = sessionId,
                UserId = userId,
                SaleId = sale.Id,
                PaymentTransactionId = payment.Id,
                Type = CashMovementType.SaleReceipt,
                PaymentMethod = PaymentMethod.Cash,
                Amount = receipt,
                Description = "Recebimento de venda em dinheiro"
            });

            state.Expected = decimal.Round(state.Expected + receipt, 2);
            state.Session.ExpectedAmount = state.Expected;
            sessions[sessionId] = state;
        }
    }

    private Task<decimal> CalculateCashExpectedAsync(Guid companyId, Guid sessionId, CancellationToken ct) =>
        db.CashMovements.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CashSessionId == sessionId && x.IsActive)
            .SumAsync(x => x.Type == CashMovementType.Withdrawal || x.Type == CashMovementType.Refund ? -x.Amount : x.Amount, ct);

    private async Task TryFinalizePendingCancellationAsync(Sale sale, CancellationToken ct)
    {
        var outstanding = await db.PaymentTransactions.AnyAsync(x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.IsActive &&
            x.Amount > x.RefundedAmount &&
            (x.Status == PaymentTransactionStatus.Captured || x.Status == PaymentTransactionStatus.PartiallyRefunded || x.Status == PaymentTransactionStatus.ReconciliationRequired), ct);
        if (outstanding) return;

        var reservations = await db.InventoryReservations.Where(x => x.CompanyId == sale.CompanyId && x.SaleId == sale.Id && x.IsActive).ToListAsync(ct);
        foreach (var reservation in reservations)
        {
            var balance = await db.InventoryBalances.FirstOrDefaultAsync(x => x.CompanyId == sale.CompanyId && x.WarehouseId == reservation.WarehouseId && x.ProductId == reservation.ProductId && x.IsActive, ct)
                ?? throw new BusinessException("Saldo de estoque da reserva não foi encontrado durante a conclusão do cancelamento.");
            var product = await db.Products.FirstAsync(x => x.Id == reservation.ProductId && x.CompanyId == sale.CompanyId && x.IsActive, ct);

            var active = reservation.ActiveQuantity;
            if (active > 0)
            {
                if (balance.Reserved < active) throw new BusinessException("Reserva inconsistente; cancelamento requer conciliação de estoque.");
                balance.Reserved -= active;
                reservation.ReleasedQuantity += active;
                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.ReleaseReason = sale.CancellationReason ?? "Cancelamento após estorno externo";
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
                        UserId = sale.UserId,
                        Type = StockMovementType.Entry,
                        Qty = restore,
                        CostUnit = balance.AverageCost,
                        OriginType = "SaleCancellation",
                        OriginId = sale.Id,
                        IdempotencyKey = key,
                        Notes = sale.CancellationReason ?? "Cancelamento após estorno externo"
                    });
                }
            }
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelledAt = DateTime.UtcNow;
        sale.UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeProvider(PaymentMethod method, string? provider)
    {
        if (method == PaymentMethod.Cash)
            return string.IsNullOrWhiteSpace(provider) ? "manual" : provider.Trim();

        if (string.IsNullOrWhiteSpace(provider) || string.Equals(provider, "manual", StringComparison.OrdinalIgnoreCase))
            return "NotConfigured";

        return provider.Trim();
    }

    private static void ValidateRetry(IReadOnlyCollection<PaymentTransaction> existing, IReadOnlyList<PaymentRequestDto> requested)
    {
        foreach (var request in requested)
        {
            var prior = existing.Single(x => string.Equals(x.IdempotencyKey, request.IdempotencyKey.Trim(), StringComparison.Ordinal));
            if (prior.Method != request.Method || prior.Amount != decimal.Round(request.Amount, 2))
                throw new BusinessException("Uma chave de idempotência já foi utilizada com forma ou valor de pagamento diferente.");
        }
    }

    private static void ApplyProviderEvidence(PaymentTransaction payment, PaymentProviderResult result, PaymentConfirmationSource source)
    {
        payment.ExternalTransactionId = result.ExternalTransactionId ?? payment.ExternalTransactionId;
        payment.Nsu = result.Nsu ?? payment.Nsu;
        payment.AuthCode = result.AuthCode ?? payment.AuthCode;
        payment.PixTxId = result.PixTxId ?? payment.PixTxId;
        payment.FailureCode = result.FailureCode;
        payment.FailureMessage = result.FailureMessage;
        payment.ProviderPayloadHash = result.RawPayloadHash;
        payment.ConfirmationSource = source;
    }

    private static PaymentTransactionDto Map(PaymentTransaction x) => new(
        x.Id, x.SaleId, x.Method, x.Amount, x.RefundedAmount, x.Status, x.ConfirmationSource,
        x.Provider, x.IdempotencyKey, x.ExternalTransactionId, x.FailureCode, x.FailureMessage,
        x.CreatedAt, x.CapturedAt, x.RefundedAt);
}
