using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Departments.Commands;
using NinaERP.Application.Features.Departments.Queries;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ISender _mediator;
    public DepartmentsController(ISender mediator) => _mediator = mediator;

    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentResponse>>>> Get(CancellationToken ct)
    {
        var departments = await _mediator.Send(new GetDepartmentsQuery(CompanyId), ct);
        return Ok(ApiResponse<IReadOnlyList<DepartmentResponse>>.Ok(departments));
    }

    [HttpPost]
    [Authorize(Policy = "InventoryWrite")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateDepartmentCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command with { CompanyId = CompanyId }, ct);
        return Created($"/api/departments/{id}", ApiResponse<Guid>.Ok(id, "Departamento criado com sucesso."));
    }
}
