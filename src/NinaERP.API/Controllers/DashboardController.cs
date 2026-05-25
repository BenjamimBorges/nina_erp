using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Dashboard.Queries;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ISender _mediator;
    public DashboardController(ISender mediator) => _mediator = mediator;
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> Get(CancellationToken ct)
    {
        var summary = await _mediator.Send(new GetDashboardQuery(CompanyId), ct);
        return Ok(ApiResponse<DashboardResponse>.Ok(summary));
    }
}
