using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Enums;

namespace NinaERP.API.Controllers;

[Authorize, ApiController, Route("api/cash")]
public sealed class CashController(ICashManagementService cash) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    public sealed record RegisterRequest(string Code, string Name);
    public sealed record OpenRequest(Guid CashRegisterId, decimal OpeningAmount, string IdempotencyKey);
    public sealed record CloseRequest(decimal CountedAmount, string? Notes, string IdempotencyKey);
    public sealed record MovementRequest(CashMovementType Type, decimal Amount, string Description, string IdempotencyKey, Guid? ApprovalRequestId = null);

    [HttpGet("registers")]
    public async Task<IActionResult> Registers(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.GetRegistersAsync(CompanyId, ct)));

    [HttpPost("registers"), Authorize(Policy = "ManagementWrite")]
    public async Task<IActionResult> CreateRegister(RegisterRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.CreateRegisterAsync(CompanyId, r.Code, r.Name, ct)));

    [HttpGet("session/open")]
    public async Task<IActionResult> OpenSession(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.GetOpenSessionAsync(CompanyId, UserId, ct)));

    [HttpPost("session/open"), Authorize(Policy = "CommercialWrite")]
    public async Task<IActionResult> Open(OpenRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.OpenSessionAsync(CompanyId, UserId, r.CashRegisterId, r.OpeningAmount, r.IdempotencyKey, ct)));

    [HttpPost("session/{id:guid}/close"), Authorize(Policy = "CommercialWrite")]
    public async Task<IActionResult> Close(Guid id, CloseRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.CloseSessionAsync(CompanyId, UserId, id, r.CountedAmount, r.Notes, r.IdempotencyKey, ct)));

    [HttpPost("session/{id:guid}/movement"), Authorize(Policy = "CommercialWrite")]
    public async Task<IActionResult> Movement(Guid id, MovementRequest r, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.AddMovementAsync(CompanyId, UserId, id, r.Type, r.Amount, r.Description, r.IdempotencyKey, r.ApprovalRequestId, ct)));

    [HttpGet("session/{id:guid}/movements")]
    public async Task<IActionResult> Movements(Guid id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await cash.GetMovementsAsync(CompanyId, id, ct)));
}
