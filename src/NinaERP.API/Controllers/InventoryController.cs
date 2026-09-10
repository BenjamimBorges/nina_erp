using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/inventory")]
public sealed class InventoryController(IInventoryService inventory):ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value); private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")!);
    public sealed record WarehouseRequest(string Code,string Name,string? BranchCode,bool IsDefault); public sealed record TransferRequest(Guid ProductId,Guid SourceWarehouseId,Guid DestinationWarehouseId,decimal Quantity,string Reason,string IdempotencyKey); public sealed record AdjustmentRequest(Guid ProductId,Guid WarehouseId,decimal NewOnHand,string Reason,string IdempotencyKey);
    [HttpGet("warehouses")] public async Task<IActionResult> Warehouses(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await inventory.GetWarehousesAsync(CompanyId,ct)));
    [HttpGet("balances")] public async Task<IActionResult> Balances([FromQuery]Guid? warehouseId,[FromQuery]string? search,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await inventory.GetBalancesAsync(CompanyId,warehouseId,search,ct)));
    [HttpPost("warehouses"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Warehouse(WarehouseRequest r,CancellationToken ct)=>Ok(ApiResponse<Guid>.Ok(await inventory.CreateWarehouseAsync(CompanyId,r.Code,r.Name,r.BranchCode,r.IsDefault,ct)));
    [HttpPost("transfer"),Authorize(Policy="InventoryWrite")] public async Task<IActionResult> Transfer(TransferRequest r,CancellationToken ct){await inventory.TransferAsync(CompanyId,UserId,r.ProductId,r.SourceWarehouseId,r.DestinationWarehouseId,r.Quantity,r.Reason,r.IdempotencyKey,ct);return NoContent();}
    [HttpPost("adjust"),Authorize(Policy="InventoryWrite")] public async Task<IActionResult> Adjust(AdjustmentRequest r,CancellationToken ct){await inventory.AdjustAsync(CompanyId,UserId,r.ProductId,r.WarehouseId,r.NewOnHand,r.Reason,r.IdempotencyKey,ct);return NoContent();}
}
