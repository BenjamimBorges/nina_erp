using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Clients.Commands;
using NinaERP.Application.Features.Clients.Queries;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISender _mediator;
    public SuppliersController(ISender mediator) => _mediator = mediator;
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierResponse>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(CompanyId, search), ct);
        return Ok(ApiResponse<IReadOnlyList<SupplierResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateSupplierCommand cmd, CancellationToken ct)
    {
        var id = await _mediator.Send(cmd with { CompanyId = CompanyId }, ct);
        return Created($"/api/suppliers/{id}", ApiResponse<Guid>.Ok(id, "Fornecedor criado com sucesso."));
    }
}
