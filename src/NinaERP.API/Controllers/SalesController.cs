using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Sales.Commands;
using NinaERP.Application.Features.Sales.Queries;
using NinaERP.Contracts.Requests;
using NinaERP.Contracts.Responses;
using NinaERP.Domain.Enums;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/[controller]")]
public sealed class SalesController(ISender mediator) : ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")!);
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleResponse>>>> Get([FromQuery]DateTime? from,[FromQuery]DateTime? to,CancellationToken ct){var end=to?.ToUniversalTime()??DateTime.UtcNow;var start=from?.ToUniversalTime()??end.AddDays(-30);if(start>end)return BadRequest(ApiResponse<IReadOnlyList<SaleResponse>>.Fail("Período inválido."));var r=await mediator.Send(new GetSalesQuery(CompanyId,start,end),ct);return Ok(ApiResponse<IReadOnlyList<SaleResponse>>.Ok(r));}
    [HttpPost,Authorize(Policy="CommercialWrite")] public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody]CreateSaleRequest r,CancellationToken ct){var items=r.Items.Select(x=>new CreateSaleItem(x.ProductId,x.Qty,x.Discount)).ToList();var id=await mediator.Send(new CreateSaleCommand(CompanyId,UserId,r.CashSessionId,r.WarehouseId,r.IdempotencyKey,r.ClientId,items),ct);return Created($"/api/sales/{id}",ApiResponse<Guid>.Ok(id,"Venda aberta com sucesso."));}
    [HttpPost("{id:guid}/finalize"),Authorize(Policy="CommercialWrite")] public async Task<IActionResult> Finalize(Guid id,[FromBody]FinalizeSaleRequest r,CancellationToken ct){var ps=r.Payments.Select(x=>new SalePaymentInput(x.Method,x.Amount,x.IdempotencyKey,x.Provider,x.ExternalTransactionId,x.Nsu,x.AuthCode,x.PixTxId,x.DueDate)).ToList();var result=await mediator.Send(new FinalizeSaleCommand(CompanyId,UserId,id,r.CashSessionId,ps),ct);return Ok(ApiResponse<object>.Ok(new{transactions=result,status=result.Any(x=>x.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Processing)?"PaymentPending":"Completed"}));}
    [HttpPost("{id:guid}/suspend"),Authorize(Policy="CommercialWrite")] public async Task<IActionResult> Suspend(Guid id,CancellationToken ct){await mediator.Send(new SuspendSaleCommand(CompanyId,UserId,id),ct);return NoContent();}
    [HttpPost("{id:guid}/resume"),Authorize(Policy="CommercialWrite")] public async Task<IActionResult> Resume(Guid id,CancellationToken ct){await mediator.Send(new ResumeSaleCommand(CompanyId,UserId,id),ct);return NoContent();}
    [HttpPost("{id:guid}/cancel"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Cancel(Guid id,[FromBody]CancelSaleRequest r,CancellationToken ct){await mediator.Send(new CancelSaleCommand(CompanyId,UserId,id,r.Reason,r.IdempotencyKey),ct);return NoContent();}
}
