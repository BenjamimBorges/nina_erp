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
public class ClientsController : ControllerBase
{
    private readonly ISender _mediator;
    public ClientsController(ISender mediator) => _mediator = mediator;
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientResponse>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetClientsQuery(CompanyId, search), ct);
        return Ok(ApiResponse<IReadOnlyList<ClientResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateClientCommand cmd, CancellationToken ct)
    {
        var id = await _mediator.Send(cmd with { CompanyId = CompanyId }, ct);
        return Created($"/api/clients/{id}", ApiResponse<Guid>.Ok(id, "Cliente criado com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd with { Id = id }, ct);
        return NoContent();
    }
}
