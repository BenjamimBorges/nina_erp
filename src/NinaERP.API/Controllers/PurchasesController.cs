using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize, ApiController, Route("api/purchases")]
public sealed class PurchasesController(IPurchaseService purchases) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
    public sealed record Item(Guid ProductId, decimal Quantity, decimal UnitCost, decimal Discount = 0, decimal Taxes = 0);
    public sealed record ReceiptItem(Guid ProductId, decimal Quantity);
    public sealed record CreateRequest(Guid SupplierId, Guid WarehouseId, string Number, IReadOnlyList<Item> Items, DateTime? ExpectedAt, string? Notes);
    public sealed record ReceiveRequest(string IdempotencyKey, IReadOnlyList<ReceiptItem>? Items, DateTime? PayableDueDate);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var end = to ?? DateTime.UtcNow;
        var start = from ?? end.AddDays(-90);
        return Ok(ApiResponse<object>.Ok(await purchases.GetAsync(CompanyId, start, end, ct)));
    }

    [HttpPost, Authorize(Policy = "ManagementWrite")]
    public async Task<IActionResult> Create(CreateRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await purchases.CreateAsync(CompanyId, UserId, r.SupplierId, r.WarehouseId, r.Number, r.Items.Select(x => new PurchaseItemInput(x.ProductId, x.Quantity, x.UnitCost, x.Discount, x.Taxes)).ToList(), r.ExpectedAt, r.Notes, ct)));

    [HttpPost("{id:guid}/receive"), Authorize(Policy = "InventoryWrite")]
    public async Task<IActionResult> Receive(Guid id, ReceiveRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await purchases.ReceiveAsync(CompanyId, UserId, id, r.IdempotencyKey, r.Items?.Select(x => new PurchaseReceiptItemInput(x.ProductId, x.Quantity)).ToList(), r.PayableDueDate, ct)));
}
