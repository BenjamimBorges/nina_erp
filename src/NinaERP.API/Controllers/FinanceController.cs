using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Enums;

namespace NinaERP.API.Controllers;

[Authorize, ApiController, Route("api/finance")]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
    public sealed record SettleRequest(decimal Amount, string IdempotencyKey, decimal Interest = 0, decimal Penalty = 0, decimal Discount = 0, PaymentMethod? PaymentMethod = null, string? ExternalReference = null);
    public sealed record ReverseRequest(string Reason, string IdempotencyKey);

    [HttpGet("payables")]
    public async Task<IActionResult> Payables([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await finance.GetPayablesAsync(CompanyId, from, to, ct)));
    [HttpGet("receivables")]
    public async Task<IActionResult> Receivables([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await finance.GetReceivablesAsync(CompanyId, from, to, ct)));
    [HttpPost("{type}/{id:guid}/settle"), Authorize(Policy = "ManagementWrite")]
    public async Task<IActionResult> Settle(FinancialTitleType type, Guid id, SettleRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await finance.SettleAsync(CompanyId, UserId, type, id, r.Amount, r.Interest, r.Penalty, r.Discount, r.PaymentMethod, r.ExternalReference, r.IdempotencyKey, ct)));
    [HttpPost("settlements/{id:guid}/reverse"), Authorize(Policy = "ManagementWrite")]
    public async Task<IActionResult> Reverse(Guid id, ReverseRequest r, CancellationToken ct) => Ok(ApiResponse<object>.Ok(await finance.ReverseSettlementAsync(CompanyId, UserId, id, r.Reason, r.IdempotencyKey, ct)));
}
