using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Enums;

namespace NinaERP.API.Controllers;

[Authorize(Policy = "ManagementWrite")]
[ApiController]
[EnableRateLimiting("omnichannel")]
[Route("api/omnichannel")]
public sealed class OmnichannelController(IOmnichannelService service, IConfiguration configuration) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private IActionResult Disabled() => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("Omnichannel está desabilitado neste ambiente."));

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog([FromQuery] int limit = 1000, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:Omnichannel", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await service.GetCatalogAsync(CompanyId, limit, ct)));
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] ChannelOrderStatus? status, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        if (!configuration.GetValue("Features:Omnichannel", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await service.GetOrdersAsync(CompanyId, status, limit, ct)));
    }

    [HttpPost("orders")]
    [RequestSizeLimit(262_144)]
    public async Task<IActionResult> Receive([FromBody] ReceiveChannelOrderInput request, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Omnichannel", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await service.ReceiveAsync(CompanyId, UserId, request, ct)));
    }

    [HttpPost("orders/{id:guid}/link-sale/{saleId:guid}")]
    public async Task<IActionResult> LinkSale(Guid id, Guid saleId, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Omnichannel", false)) return Disabled();
        return Ok(ApiResponse<object>.Ok(await service.MarkImportedAsync(CompanyId, id, saleId, ct)));
    }
}
