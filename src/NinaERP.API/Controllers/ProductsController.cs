using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Products.Commands;
using NinaERP.Application.Features.Products.Queries;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;

    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductResponse>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsQuery(CompanyId, search), ct);
        return Ok(ApiResponse<IReadOnlyList<ProductResponse>>.Ok(result));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductResponse>>>> GetLowStock(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLowStockQuery(CompanyId), ct);
        return Ok(ApiResponse<IReadOnlyList<ProductResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateProductCommand cmd, CancellationToken ct)
    {
        var command = cmd with { CompanyId = CompanyId };
        var id = await _mediator.Send(command, ct);
        return Created($"/api/products/{id}", ApiResponse<Guid>.Ok(id, "Produto criado com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProductCommand(id), ct);
        return NoContent();
    }
}
