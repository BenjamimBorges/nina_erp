using Microsoft.EntityFrameworkCore;
using NinaERP.Application.Common.Exceptions;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.Infrastructure.Services;

public sealed class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(Guid companyId, CancellationToken ct) =>
        await db.Warehouses.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.BranchCode, x.IsDefault))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InventoryBalanceDto>> GetBalancesAsync(
        Guid companyId,
        Guid? warehouseId,
        string? search,
        CancellationToken ct)
    {
        if (warehouseId.HasValue && !await db.Warehouses.AsNoTracking()
                .AnyAsync(x => x.Id == warehouseId.Value && x.CompanyId == companyId && x.IsActive, ct))
            throw new BusinessException("Depósito não encontrado para a empresa autenticada.");

        var query = db.InventoryBalances.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == companyId && x.IsActive);

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Product!.Name.ToLower().Contains(term) ||
                x.Product.Sku.ToLower().Contains(term) ||
                x.Product.Barcode.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.Product!.Name)
            .Take(1000)
            .Select(x => new InventoryBalanceDto(
                x.ProductId,
                x.Product!.Sku,
                x.Product.Name,
                x.WarehouseId,
                x.Warehouse!.Name,
                x.OnHand,
                x.Reserved,
                x.OnHand - x.Reserved,
                x.AverageCost,
                x.Product.StockMin))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateWarehouseAsync(
        Guid companyId,
        string code,
        string name,
        string? branchCode,
        bool isDefault,
        CancellationToken ct)
    {
        code = code?.Trim().ToUpperInvariant() ?? string.Empty;
        name = name?.Trim() ?? string.Empty;
        branchCode = string.IsNullOrWhiteSpace(branchCode) ? null : branchCode.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code) || code.Length > 30)
            throw new BusinessException("Código do depósito é obrigatório e deve ter até 30 caracteres.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
            throw new BusinessException("Nome do depósito é obrigatório e deve ter até 120 caracteres.");
        if (branchCode?.Length > 30)
            throw new BusinessException("Código da filial deve ter até 30 caracteres.");

        if (await db.Warehouses.AnyAsync(x => x.CompanyId == companyId && x.Code == code, ct))
            throw new BusinessException("Código de depósito já existe.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (isDefault)
        {
            var defaults = await db.Warehouses
                .Where(x => x.CompanyId == companyId && x.IsDefault && x.IsActive)
                .ToListAsync(ct);
            foreach (var warehouse in defaults)
                warehouse.IsDefault = false;
        }

        var warehouse = new Warehouse
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            BranchCode = branchCode,
            IsDefault = isDefault
        };
        db.Warehouses.Add(warehouse);

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("Não foi possível criar o depósito. Código ou depósito padrão podem ter sido alterados simultaneamente.");
        }

        return warehouse.Id;
    }

    public async Task TransferAsync(
        Guid companyId,
        Guid userId,
        Guid productId,
        Guid sourceWarehouseId,
        Guid destinationWarehouseId,
        decimal quantity,
        string reason,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            throw new BusinessException("Origem e destino devem ser diferentes.");
        if (quantity <= 0)
            throw new BusinessException("Quantidade inválida.");

        reason = RequiredReason(reason);
        idempotencyKey = RequiredKey(idempotencyKey);
        var requestHash = $"{productId:N}:{sourceWarehouseId:N}:{destinationWarehouseId:N}:{quantity:0.####}:{reason}";
        var prior = await FindIdempotencyAsync(companyId, "InventoryTransfer", idempotencyKey, requestHash, ct);
        if (prior is not null)
            return;

        var validWarehouses = await db.Warehouses.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive &&
                        (x.Id == sourceWarehouseId || x.Id == destinationWarehouseId))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (validWarehouses.Count != 2)
            throw new BusinessException("Depósito de origem ou destino não pertence à empresa autenticada.");

        var product = await db.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Produto não encontrado para a empresa autenticada.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var source = await db.InventoryBalances.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.WarehouseId == sourceWarehouseId && x.ProductId == productId && x.IsActive,
            ct) ?? throw new BusinessException("Saldo de origem não encontrado.");

        if (source.Available < quantity)
            throw new BusinessException("Saldo disponível insuficiente para transferência.");

        var destination = await db.InventoryBalances.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.WarehouseId == destinationWarehouseId && x.ProductId == productId && x.IsActive,
            ct);

        if (destination is null)
        {
            destination = new InventoryBalance
            {
                CompanyId = companyId,
                WarehouseId = destinationWarehouseId,
                ProductId = productId,
                AverageCost = source.AverageCost
            };
            db.InventoryBalances.Add(destination);
        }

        source.OnHand -= quantity;
        var destinationOldQty = destination.OnHand;
        var destinationNewQty = destinationOldQty + quantity;
        destination.AverageCost = destinationNewQty <= 0
            ? source.AverageCost
            : decimal.Round(
                ((destinationOldQty * destination.AverageCost) + (quantity * source.AverageCost)) / destinationNewQty,
                4);
        destination.OnHand = destinationNewQty;

        var movementPrefix = $"transfer:{idempotencyKey}";
        db.StockMovements.AddRange(
            new StockMovement
            {
                CompanyId = companyId,
                ProductId = product.Id,
                WarehouseId = sourceWarehouseId,
                CounterpartyWarehouseId = destinationWarehouseId,
                UserId = userId,
                Type = StockMovementType.Transfer,
                Qty = quantity,
                CostUnit = source.AverageCost,
                OriginType = "WarehouseTransfer",
                IdempotencyKey = movementPrefix + ":out",
                Notes = reason
            },
            new StockMovement
            {
                CompanyId = companyId,
                ProductId = product.Id,
                WarehouseId = destinationWarehouseId,
                CounterpartyWarehouseId = sourceWarehouseId,
                UserId = userId,
                Type = StockMovementType.Transfer,
                Qty = quantity,
                CostUnit = source.AverageCost,
                OriginType = "WarehouseTransfer",
                IdempotencyKey = movementPrefix + ":in",
                Notes = reason
            });

        db.IdempotencyRecords.Add(NewIdempotency(
            companyId, "InventoryTransfer", idempotencyKey, requestHash, null));

        await SaveTransactionalAsync(tx, "A transferência sofreu conflito de concorrência. Recarregue os saldos e tente novamente.", ct);
    }

    public async Task AdjustAsync(
        Guid companyId,
        Guid userId,
        Guid productId,
        Guid warehouseId,
        decimal newOnHand,
        string reason,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (newOnHand < 0)
            throw new BusinessException("Saldo físico não pode ser negativo.");

        reason = RequiredReason(reason);
        idempotencyKey = RequiredKey(idempotencyKey);
        var requestHash = $"{productId:N}:{warehouseId:N}:{newOnHand:0.####}:{reason}";
        var prior = await FindIdempotencyAsync(companyId, "InventoryAdjustment", idempotencyKey, requestHash, ct);
        if (prior is not null)
            return;

        if (!await db.Warehouses.AsNoTracking().AnyAsync(
                x => x.Id == warehouseId && x.CompanyId == companyId && x.IsActive, ct))
            throw new BusinessException("Depósito não encontrado para a empresa autenticada.");

        var product = await db.Products.FirstOrDefaultAsync(
            x => x.Id == productId && x.CompanyId == companyId && x.IsActive, ct)
            ?? throw new BusinessException("Produto não encontrado para a empresa autenticada.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var balance = await db.InventoryBalances.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.WarehouseId == warehouseId && x.ProductId == productId && x.IsActive,
            ct);

        if (balance is null)
        {
            balance = new InventoryBalance
            {
                CompanyId = companyId,
                WarehouseId = warehouseId,
                ProductId = productId,
                AverageCost = product.CostAverage
            };
            db.InventoryBalances.Add(balance);
        }

        if (newOnHand < balance.Reserved)
            throw new BusinessException("Novo saldo não pode ser menor que o estoque reservado.");

        var delta = newOnHand - balance.OnHand;
        balance.OnHand = newOnHand;
        product.StockQty += delta;
        if (product.StockQty < 0)
            throw new BusinessException("Ajuste geraria saldo agregado negativo.");
        product.UpdatedAt = DateTime.UtcNow;

        if (delta != 0)
        {
            db.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                ProductId = productId,
                WarehouseId = warehouseId,
                UserId = userId,
                Type = StockMovementType.Inventory,
                Qty = Math.Abs(delta),
                CostUnit = balance.AverageCost,
                OriginType = "InventoryCount",
                IdempotencyKey = $"inventory:{idempotencyKey}",
                Notes = $"{reason} | diferença {delta:0.####}"
            });
        }

        db.IdempotencyRecords.Add(NewIdempotency(
            companyId, "InventoryAdjustment", idempotencyKey, requestHash, null));

        await SaveTransactionalAsync(tx, "O ajuste sofreu conflito de concorrência. Recarregue o saldo e tente novamente.", ct);
    }

    private async Task<IdempotencyRecord?> FindIdempotencyAsync(
        Guid companyId,
        string scope,
        string key,
        string requestHash,
        CancellationToken ct)
    {
        var prior = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Scope == scope && x.Key == key, ct);
        if (prior is not null && !string.Equals(prior.RequestHash, requestHash, StringComparison.Ordinal))
            throw new BusinessException("A chave de idempotência já foi utilizada com parâmetros diferentes.");
        return prior;
    }

    private async Task SaveTransactionalAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx,
        string concurrencyMessage,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException(concurrencyMessage);
        }
        catch (DbUpdateException ex) when (ex.InnerException is not null)
        {
            await tx.RollbackAsync(ct);
            throw new BusinessException("A operação de estoque violou uma restrição de integridade. Recarregue os dados antes de repetir.");
        }
    }

    private static string RequiredKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessException("Chave de idempotência é obrigatória.");
        var value = key.Trim();
        if (value.Length > 120)
            throw new BusinessException("Chave de idempotência deve ter até 120 caracteres.");
        return value;
    }

    private static string RequiredReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new BusinessException("Informe uma justificativa com ao menos 5 caracteres.");
        var value = reason.Trim();
        if (value.Length > 300)
            throw new BusinessException("Justificativa deve ter até 300 caracteres.");
        return value;
    }

    private static IdempotencyRecord NewIdempotency(
        Guid companyId,
        string scope,
        string key,
        string hash,
        Guid? resourceId) => new()
    {
        CompanyId = companyId,
        Scope = scope,
        Key = key,
        RequestHash = hash,
        ResourceId = resourceId,
        ExpiresAt = DateTime.UtcNow.AddYears(5)
    };
}
