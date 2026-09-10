using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/cluster")]
public sealed class ClusterController(IClusterCoordinationService cluster, IConfiguration configuration) : ControllerBase
{
    [HttpGet("nodes")]
    public async Task<IActionResult> Nodes(CancellationToken ct)
    {
        if (!configuration.GetValue("Features:DistributedScale", false))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("Coordenação distribuída está desabilitada neste ambiente."));
        return Ok(ApiResponse<object>.Ok(await cluster.GetNodesAsync(TimeSpan.FromSeconds(60), ct)));
    }
}
