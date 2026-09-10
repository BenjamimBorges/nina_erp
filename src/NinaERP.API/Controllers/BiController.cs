using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "ManagementWrite")]
[ApiController]
[Route("api/bi")]
public sealed class BiController(IAdvancedBiService bi, IConfiguration configuration) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private IActionResult Disabled() => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("BI avançado está desabilitado neste ambiente."));

    [HttpGet("executive")]
    public async Task<IActionResult> Executive([FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:AdvancedBI", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await bi.GetExecutiveSummaryAsync(CompanyId, Math.Clamp(months, 1, 24), ct)));
    }

    [HttpGet("sellers")]
    public async Task<IActionResult> Sellers([FromQuery] int months = 12, [FromQuery] int limit = 200, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:AdvancedBI", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await bi.GetSellersAsync(CompanyId, Math.Clamp(months, 1, 24), limit, ct)));
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:AdvancedBI", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await bi.GetPaymentMixAsync(CompanyId, Math.Clamp(months, 1, 24), ct)));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!configuration.GetValue("Features:AdvancedBI", false)) return Disabled();
        await bi.RefreshAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { refreshedAt = DateTime.UtcNow }));
    }
}
