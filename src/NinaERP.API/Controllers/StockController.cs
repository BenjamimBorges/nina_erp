using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Stock.Commands;
using NinaERP.Application.Features.Stock.Queries;
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

    [HttpGet("movements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockMovementResponse>>>> GetMovements(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? productId,
        CancellationToken ct)
    {
        var until = to ?? DateTime.UtcNow;
        var since = from ?? until.AddDays(-30);
        if (since > until) return BadRequest(ApiResponse<string>.Fail("Período inválido."));

        var result = await _mediator.Send(new GetStockMovementsQuery(CompanyId, since, until, productId), ct);
        return Ok(ApiResponse<IReadOnlyList<StockMovementResponse>>.Ok(result));
    }

    [HttpPost("adjust")]
    [Authorize(Policy = "InventoryWrite")]
    public async Task<ActionResult<ApiResponse<Guid>>> Adjust([FromBody] AdjustStockCommand cmd, CancellationToken ct)
    {
        var id = await _mediator.Send(cmd with { CompanyId = CompanyId }, ct);
        return Ok(ApiResponse<Guid>.Ok(id, "Ajuste de estoque registrado."));
    }
}
