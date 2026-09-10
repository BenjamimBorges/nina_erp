using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/pos/devices")]
public sealed class PosDevicesController(IPointOfSaleDeviceService devices):ControllerBase
{
    [HttpGet("status")] public async Task<IActionResult> Status(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await devices.GetStatusAsync(ct)));
    [HttpPost("print/{saleId:guid}"),Authorize(Policy="CommercialWrite")] public async Task<IActionResult> Print(Guid saleId,CancellationToken ct){await devices.PrintReceiptAsync(saleId,ct);return NoContent();}
    [HttpPost("cash-drawer/open"),Authorize(Policy="CommercialWrite")] public async Task<IActionResult> Drawer(CancellationToken ct){await devices.OpenCashDrawerAsync(ct);return NoContent();}
}
