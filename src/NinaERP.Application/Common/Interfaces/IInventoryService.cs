namespace NinaERP.Application.Common.Interfaces;
public sealed record WarehouseDto(Guid Id,string Code,string Name,string? BranchCode,bool IsDefault);
public sealed record InventoryBalanceDto(Guid ProductId,string Sku,string ProductName,Guid WarehouseId,string WarehouseName,decimal OnHand,decimal Reserved,decimal Available,decimal AverageCost,decimal Minimum);
public interface IInventoryService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(Guid companyId,CancellationToken ct);
    Task<IReadOnlyList<InventoryBalanceDto>> GetBalancesAsync(Guid companyId,Guid? warehouseId,string? search,CancellationToken ct);
    Task<Guid> CreateWarehouseAsync(Guid companyId,string code,string name,string? branchCode,bool isDefault,CancellationToken ct);
    Task TransferAsync(Guid companyId,Guid userId,Guid productId,Guid sourceWarehouseId,Guid destinationWarehouseId,decimal quantity,string reason,string idempotencyKey,CancellationToken ct);
    Task AdjustAsync(Guid companyId,Guid userId,Guid productId,Guid warehouseId,decimal newOnHand,string reason,string idempotencyKey,CancellationToken ct);
}
