using MediatR;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Suppliers.Commands.CreateSupplier;
using NinaERP.Application.Features.Suppliers.Commands.DeleteSupplier;
using NinaERP.Application.Features.Suppliers.Commands.UpdateSupplier;
using NinaERP.Application.Features.Suppliers.Queries.GetAllSuppliers;
using NinaERP.Application.Features.Suppliers.Queries.GetSupplierById;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISender _mediator;

    public SuppliersController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateSupplierCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result },
            ApiResponse<Guid>.Ok(result, "Fornecedor criado com sucesso."));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> GetById(Guid id)
    {
        var query = new GetSupplierByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<SupplierResponse>.Ok(result, "Fornecedor recuperado com sucesso."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierResponse>>>> GetAll([FromQuery] Guid companyId)
    {
        var query = new GetAllSuppliersQuery(companyId);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<List<SupplierResponse>>.Ok(result, "Fornecedores recuperados com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(Guid id, [FromBody] UpdateSupplierCommand command)
    {
        // Ensure ID from route matches the command
        var updateCommand = command with { Id = id };
        await _mediator.Send(updateCommand);
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Fornecedor atualizado com sucesso."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id)
    {
        var command = new DeleteSupplierCommand(id);
        await _mediator.Send(command);
        return Ok(ApiResponse<Unit>.Ok(Unit.Value, "Fornecedor deletado com sucesso."));
    }
}
