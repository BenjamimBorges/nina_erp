using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Stock.Commands;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly ISender _mediator;
    public StockController(ISender mediator) => _mediator = mediator;
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpPost("adjust")]
    public async Task<ActionResult<ApiResponse<Guid>>> Adjust([FromBody] AdjustStockCommand cmd, CancellationToken ct)
    {
        var id = await _mediator.Send(cmd with { CompanyId = CompanyId }, ct);
        return Ok(ApiResponse<Guid>.Ok(id, "Ajuste de estoque registrado."));
    }
}
