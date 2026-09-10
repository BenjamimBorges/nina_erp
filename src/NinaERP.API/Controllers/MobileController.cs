using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/mobile")]
public sealed class MobileController(IMobileService mobile, IConfiguration configuration) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private IActionResult Disabled() => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("API móvel está desabilitada neste ambiente."));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (!configuration.GetValue("Features:MobileApi", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await mobile.GetSummaryAsync(CompanyId, UserId, ct)));
    }

    [HttpGet("sales/recent")]
    public async Task<IActionResult> RecentSales([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:MobileApi", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await mobile.GetRecentSalesAsync(CompanyId, limit, ct)));
    }

    [HttpGet("stock/low")]
    public async Task<IActionResult> LowStock([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:MobileApi", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await mobile.GetLowStockAsync(CompanyId, limit, ct)));
    }
}
