using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Departments.Commands;
using NinaERP.Application.Features.Departments.Queries;
using NinaERP.Contracts;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentResponse>>>> Get()
    {
        var departments = await _mediator.Send(new GetDepartmentsQuery());
        return Ok(new ApiResponse<IEnumerable<DepartmentResponse>>
        {
            Success = true,
            Message = "Departamentos recuperados com sucesso",
            Data = departments
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateDepartmentCommand cmd)
    {
        var departmentId = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(Get), new ApiResponse<Guid>
        {
            Success = true,
            Message = "Departamento criado com sucesso",
            Data = departmentId
        });
    }
}
