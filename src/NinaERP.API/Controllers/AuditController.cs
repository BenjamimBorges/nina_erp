using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Audit.Queries;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _mediator;
    public AuditController(ISender mediator) => _mediator = mediator;
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditLogResponse>>>> Get([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(CompanyId, Math.Clamp(take, 1, 500)), ct);
        return Ok(ApiResponse<IReadOnlyList<AuditLogResponse>>.Ok(result));
    }
}
