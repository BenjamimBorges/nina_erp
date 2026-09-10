using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Entities;
using NinaERP.Domain.Enums;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/approvals")]
public sealed class ApprovalsController(IApprovalService approvals, IConfiguration configuration) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private IActionResult Disabled() => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("Fluxo de aprovações está desabilitado neste ambiente."));

    public sealed record CreateApprovalRequest(ApprovalType Type, string ResourceType, Guid ResourceId, UserRole RequiredRole,
        string Reason, string? PayloadJson, string IdempotencyKey, DateTime? ExpiresAt);
    public sealed record DecideApprovalRequest(bool Approve, string? Notes);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ApprovalStatus? status, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Approvals", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await approvals.GetAsync(CompanyId, status, ct)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApprovalRequest request, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Approvals", false)) return Disabled();
        var result = await approvals.RequestAsync(CompanyId, UserId, request.Type, request.ResourceType, request.ResourceId,
            request.RequiredRole, request.Reason, request.PayloadJson, request.IdempotencyKey, request.ExpiresAt, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<IActionResult> Decide(Guid id, [FromBody] DecideApprovalRequest request, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Approvals", false)) return Disabled();
        var result = await approvals.DecideAsync(CompanyId, UserId, id, request.Approve, request.Notes, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
