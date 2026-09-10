using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class PurchaseService(AppDbContext db) : IPurchaseService
{
    public async Task<PurchaseOrderDto> CreateAsync(Guid companyId, Guid userId, Guid supplierId, Guid warehouseId, string number, IReadOnlyList<PurchaseItemInput> items, DateTime? expectedAt, string? notes, CancellationToken ct)
    {
        if (items.Count == 0) throw new BusinessException("O pedido de compra precisa de itens.");
        if (await db.PurchaseOrders.AnyAsync(x => x.CompanyId == companyId && x.Number == number.Trim(), ct)) throw new BusinessException("Número de pedido já utilizado.");
        var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Fornecedor não encontrado.");
        _ = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == warehouseId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Depósito não encontrado.");
        var productIds = items.Select(x => x.ProductId).Distinct().ToArray();
        var validProducts = await db.Products.Where(x => x.CompanyId == companyId && productIds.Contains(x.Id) && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        if (validProducts.Count != productIds.Length) throw new BusinessException("Há produto inválido ou pertencente a outra empresa no pedido.");
        if (items.Any(x => x.Quantity <= 0 || x.UnitCost < 0 || x.Discount < 0 || x.Taxes < 0)) throw new BusinessException("Quantidade, custo, desconto ou tributos inválidos.");

        var order = new PurchaseOrder { CompanyId = companyId, SupplierId = supplierId, CreatedByUserId = userId, WarehouseId = warehouseId, Number = number.Trim(), Status = PurchaseOrderStatus.Open, ExpectedAt = expectedAt?.ToUniversalTime(), Notes = notes?.Trim() };
        foreach (var i in items) order.Items.Add(new PurchaseOrderItem { ProductId = i.ProductId, OrderedQty = i.Quantity, UnitCost = i.UnitCost, Discount = i.Discount, Taxes = i.Taxes });
        order.TotalProducts = items.Sum(x => x.Quantity * x.UnitCost);
        order.TotalDiscount = items.Sum(x => x.Discount);
        order.TotalTaxes = items.Sum(x => x.Taxes);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return Map(order, supplier.Name);
    }

    public async Task<PurchaseOrderDto> ReceiveAsync(Guid companyId, Guid userId, Guid purchaseOrderId, string idempotencyKey, IReadOnlyList<PurchaseReceiptItemInput>? items, DateTime? payableDueDate, CancellationToken ct)
    {
        idempotencyKey = idempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new BusinessException("Chave de idempotência obrigatória.");
        if (items is { Count: > 0 } && items.Any(x => x.Quantity <= 0)) throw new BusinessException("As quantidades recebidas devem ser positivas.");
        if (items is { Count: > 0 } && items.GroupBy(x => x.ProductId).Any(g => g.Count() > 1)) throw new BusinessException("Não repita o mesmo produto no recebimento parcial.");

        var normalized = items is { Count: > 0 }
            ? string.Join(';', items.OrderBy(x => x.ProductId).Select(x => $"{x.ProductId:N}:{x.Quantity:0.####}"))
            : "ALL_REMAINING";
        var requestHash = $"{purchaseOrderId:N}:{normalized}:{payableDueDate?.ToUniversalTime():O}";
        var existing = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == "PurchaseReceipt" && x.Key == idempotencyKey, ct);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal)) throw new BusinessException("A chave de idempotência já foi utilizada para outro recebimento.");
            if (existing.ResourceId is Guid existingId)
            {
                var previous = await db.PurchaseOrders.AsNoTracking().Include(x => x.Supplier).FirstAsync(x => x.Id == existingId && x.CompanyId == companyId, ct);
                return Map(previous, previous.Supplier?.Name ?? string.Empty);
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await db.PurchaseOrders.Include(x => x.Supplier).Include(x => x.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == purchaseOrderId && x.CompanyId == companyId && x.IsActive, ct) ?? throw new BusinessException("Pedido de compra não encontrado.");
        if (order.Status == PurchaseOrderStatus.Cancelled) throw new BusinessException("Pedido cancelado não pode ser recebido.");
        if (order.Status == PurchaseOrderStatus.Received) throw new BusinessException("Pedido já foi integralmente recebido.");

        var requestedByProduct = items is { Count: > 0 }
            ? items.ToDictionary(x => x.ProductId, x => x.Quantity)
            : order.Items.Where(x => x.ReceivedQty < x.OrderedQty).ToDictionary(x => x.ProductId, x => x.OrderedQty - x.ReceivedQty);

        if (requestedByProduct.Count == 0) throw new BusinessException("Não há itens pendentes para recebimento.");
        var orderProductIds = order.Items.Select(x => x.ProductId).ToHashSet();
        if (requestedByProduct.Keys.Any(x => !orderProductIds.Contains(x))) throw new BusinessException("O recebimento contém produto que não pertence ao pedido.");

        foreach (var item in order.Items.Where(x => requestedByProduct.ContainsKey(x.ProductId)))
        {
            var qty = requestedByProduct[item.ProductId];
            var remaining = item.OrderedQty - item.ReceivedQty;
            if (qty > remaining) throw new BusinessException($"A quantidade recebida de '{item.Product?.Name ?? item.ProductId.ToString()}' excede o saldo pendente.");

            var balance = await db.InventoryBalances.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.WarehouseId == order.WarehouseId && x.ProductId == item.ProductId, ct);
            if (balance is null)
            {
                balance = new InventoryBalance { CompanyId = companyId, WarehouseId = order.WarehouseId, ProductId = item.ProductId, OnHand = 0, AverageCost = item.UnitCost };
                db.InventoryBalances.Add(balance);
            }

            var oldQty = balance.OnHand;
            var newQty = oldQty + qty;
            balance.AverageCost = newQty == 0 ? item.UnitCost : decimal.Round(((oldQty * balance.AverageCost) + (qty * item.UnitCost)) / newQty, 4);
            balance.OnHand = newQty;

            var product = item.Product!;
            var aggregateOld = product.StockQty;
            var aggregateNew = aggregateOld + qty;
            product.CostAverage = aggregateNew == 0 ? item.UnitCost : decimal.Round(((aggregateOld * product.CostAverage) + (qty * item.UnitCost)) / aggregateNew, 4);
            product.StockQty = aggregateNew;
            product.UpdatedAt = DateTime.UtcNow;
            item.ReceivedQty += qty;

            db.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                ProductId = item.ProductId,
                WarehouseId = order.WarehouseId,
                UserId = userId,
                Type = StockMovementType.Entry,
                Qty = qty,
                CostUnit = item.UnitCost,
                OriginType = "PurchaseOrder",
                OriginId = order.Id,
                IdempotencyKey = $"{idempotencyKey}:{item.Id:N}",
                Notes = $"Recebimento do pedido {order.Number}"
            });
        }

        order.Status = order.Items.All(x => x.ReceivedQty >= x.OrderedQty) ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        if (order.Status == PurchaseOrderStatus.Received)
        {
            order.ReceivedAt = DateTime.UtcNow;
            if (!await db.AccountsPayable.AnyAsync(x => x.CompanyId == companyId && x.PurchaseOrderId == order.Id && x.IsActive, ct))
            {
                var total = order.TotalProducts - order.TotalDiscount + order.TotalFreight + order.TotalTaxes;
                db.AccountsPayable.Add(new AccountPayable
                {
                    CompanyId = companyId,
                    SupplierId = order.SupplierId,
                    PurchaseOrderId = order.Id,
                    DocumentNumber = order.Number,
                    Description = $"Pedido de compra {order.Number}",
                    OriginalAmount = total,
                    OpenAmount = total,
                    DueDate = payableDueDate?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(30),
                    OriginType = "PurchaseOrder",
                    OriginId = order.Id
                });
            }
        }

        db.IdempotencyRecords.Add(new IdempotencyRecord { CompanyId = companyId, Scope = "PurchaseReceipt", Key = idempotencyKey, RequestHash = requestHash, ResourceId = order.Id, ExpiresAt = DateTime.UtcNow.AddYears(5) });
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("O pedido foi recebido por outra operação simultânea. Recarregue o pedido antes de tentar novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Idempotency", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new BusinessException("O recebimento já foi processado por outra requisição com a mesma chave de idempotência.");
        }
        return Map(order, order.Supplier?.Name ?? string.Empty);
    }

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct) =>
        await db.PurchaseOrders.AsNoTracking().Include(x => x.Supplier).Where(x => x.CompanyId == companyId && x.OrderedAt >= from && x.OrderedAt <= to && x.IsActive)
            .OrderByDescending(x => x.OrderedAt).Take(500)
            .Select(x => new PurchaseOrderDto(x.Id, x.Number, x.SupplierId, x.Supplier!.Name, x.WarehouseId, x.Status, x.TotalProducts - x.TotalDiscount + x.TotalFreight + x.TotalTaxes, x.OrderedAt, x.ExpectedAt, x.ReceivedAt)).ToListAsync(ct);

    private static PurchaseOrderDto Map(PurchaseOrder x, string supplierName) => new(x.Id, x.Number, x.SupplierId, supplierName, x.WarehouseId, x.Status, x.TotalProducts - x.TotalDiscount + x.TotalFreight + x.TotalTaxes, x.OrderedAt, x.ExpectedAt, x.ReceivedAt);
}

public sealed class FinanceService(AppDbContext db) : IFinanceService
{
    public Task<IReadOnlyList<FinancialTitleDto>> GetPayablesAsync(Guid companyId, DateTime? dueFrom, DateTime? dueTo, CancellationToken ct) => GetTitlesAsync<AccountPayable>(companyId, FinancialTitleType.Payable, dueFrom, dueTo, ct);
    public Task<IReadOnlyList<FinancialTitleDto>> GetReceivablesAsync(Guid companyId, DateTime? dueFrom, DateTime? dueTo, CancellationToken ct) => GetTitlesAsync<AccountReceivable>(companyId, FinancialTitleType.Receivable, dueFrom, dueTo, ct);

    public async Task<FinancialSettlementDto> SettleAsync(Guid companyId, Guid userId, FinancialTitleType titleType, Guid titleId, decimal amount, decimal interest, decimal penalty, decimal discount, PaymentMethod? paymentMethod, string? externalReference, string idempotencyKey, CancellationToken ct)
    {
        if (amount <= 0 || interest < 0 || penalty < 0 || discount < 0) throw new BusinessException("Valores da baixa financeira são inválidos.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new BusinessException("Chave de idempotência obrigatória.");
        idempotencyKey = idempotencyKey.Trim();
        var hash = $"{titleType}:{titleId:N}:{amount:0.00}:{interest:0.00}:{penalty:0.00}:{discount:0.00}:{paymentMethod}:{externalReference?.Trim()}";
        var prior = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == "FinancialSettlement" && x.Key == idempotencyKey, ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.RequestHash, hash, StringComparison.Ordinal)) throw new BusinessException("A chave de idempotência já foi utilizada para outra baixa financeira.");
            if (prior.ResourceId is Guid settlementId)
            {
                var previous = await db.FinancialSettlements.AsNoTracking().FirstAsync(x => x.Id == settlementId && x.CompanyId == companyId, ct);
                return Map(previous);
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var title = await FindTitleAsync(companyId, titleType, titleId, ct) ?? throw new BusinessException("Título financeiro não encontrado.");
        if (title.Status is FinancialTitleStatus.Paid or FinancialTitleStatus.Cancelled or FinancialTitleStatus.Reversed) throw new BusinessException("Título não está aberto para baixa.");
        var resolvedPrincipal = amount + discount;
        if (resolvedPrincipal > title.OpenAmount) throw new BusinessException("A baixa excede o saldo em aberto.");
        var type = titleType == FinancialTitleType.Payable ? FinancialSettlementType.Payment : FinancialSettlementType.Receipt;
        var settlement = new FinancialSettlement { CompanyId = companyId, TitleType = titleType, TitleId = title.Id, UserId = userId, Type = type, PaymentMethod = paymentMethod, Amount = amount, Interest = interest, Penalty = penalty, Discount = discount, ExternalReference = externalReference?.Trim() };
        db.FinancialSettlements.Add(settlement);
        title.PaidAmount += amount;
        title.OpenAmount -= resolvedPrincipal;
        title.Status = title.OpenAmount == 0 ? FinancialTitleStatus.Paid : FinancialTitleStatus.PartiallyPaid;
        title.UpdatedAt = DateTime.UtcNow;
        db.IdempotencyRecords.Add(new IdempotencyRecord { CompanyId = companyId, Scope = "FinancialSettlement", Key = idempotencyKey, RequestHash = hash, ResourceId = settlement.Id, ExpiresAt = DateTime.UtcNow.AddYears(5) });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(settlement);
    }

    public async Task<FinancialSettlementDto> ReverseSettlementAsync(Guid companyId, Guid userId, Guid settlementId, string reason, string idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new BusinessException("Informe o motivo do estorno.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new BusinessException("Chave de idempotência obrigatória.");
        idempotencyKey = idempotencyKey.Trim();
        var hash = $"{settlementId:N}:{reason.Trim()}";
        var prior = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == "FinancialSettlementReversal" && x.Key == idempotencyKey, ct);
        if (prior is not null)
        {
            if (!string.Equals(prior.RequestHash, hash, StringComparison.Ordinal)) throw new BusinessException("A chave de idempotência já foi utilizada para outro estorno financeiro.");
            if (prior.ResourceId is Guid reversalId)
            {
                var previous = await db.FinancialSettlements.AsNoTracking().FirstAsync(x => x.Id == reversalId && x.CompanyId == companyId, ct);
                return Map(previous);
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var original = await db.FinancialSettlements.Include(x => x.Title).FirstOrDefaultAsync(x => x.Id == settlementId && x.CompanyId == companyId && x.Type != FinancialSettlementType.Reversal && x.IsActive, ct) ?? throw new BusinessException("Baixa não encontrada.");
        if (await db.FinancialSettlements.AnyAsync(x => x.CompanyId == companyId && x.ReversesSettlementId == original.Id && x.IsActive, ct)) throw new BusinessException("Esta baixa já foi estornada.");
        var title = original.Title ?? throw new BusinessException("Título de origem não encontrado.");
        title.PaidAmount = Math.Max(0m, title.PaidAmount - original.Amount);
        title.OpenAmount = Math.Min(title.OriginalAmount, title.OpenAmount + original.Amount + original.Discount);
        title.Status = title.OpenAmount == title.OriginalAmount ? FinancialTitleStatus.Open : FinancialTitleStatus.PartiallyPaid;
        title.UpdatedAt = DateTime.UtcNow;
        var reversal = new FinancialSettlement { CompanyId = companyId, TitleType = original.TitleType, TitleId = original.TitleId, UserId = userId, Type = FinancialSettlementType.Reversal, PaymentMethod = original.PaymentMethod, Amount = original.Amount, Interest = original.Interest, Penalty = original.Penalty, Discount = original.Discount, ReversesSettlementId = original.Id, Notes = reason.Trim() };
        db.FinancialSettlements.Add(reversal);
        db.IdempotencyRecords.Add(new IdempotencyRecord { CompanyId = companyId, Scope = "FinancialSettlementReversal", Key = idempotencyKey, RequestHash = hash, ResourceId = reversal.Id, ExpiresAt = DateTime.UtcNow.AddYears(5) });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Map(reversal);
    }

    private async Task<IReadOnlyList<FinancialTitleDto>> GetTitlesAsync<T>(Guid companyId, FinancialTitleType type, DateTime? from, DateTime? to, CancellationToken ct) where T : FinancialTitle
    {
        IQueryable<T> q = db.Set<T>().AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
        if (from.HasValue) q = q.Where(x => x.DueDate >= from.Value.ToUniversalTime());
        if (to.HasValue) q = q.Where(x => x.DueDate <= to.Value.ToUniversalTime());
        return await q.OrderBy(x => x.DueDate).Take(1000).Select(x => new FinancialTitleDto(x.Id, type, x.DocumentNumber, x.Description, x.OriginalAmount, x.OpenAmount, x.PaidAmount, x.DueDate, x.Status, x.OriginType, x.OriginId)).ToListAsync(ct);
    }
    private async Task<FinancialTitle?> FindTitleAsync(Guid companyId, FinancialTitleType type, Guid id, CancellationToken ct) => type == FinancialTitleType.Payable
        ? await db.AccountsPayable.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.IsActive, ct)
        : await db.AccountsReceivable.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.IsActive, ct);
    private static FinancialSettlementDto Map(FinancialSettlement x) => new(x.Id, x.TitleType, x.TitleId, x.Type, x.Amount, x.Interest, x.Penalty, x.Discount, x.OccurredAt, x.ReversesSettlementId);
}
