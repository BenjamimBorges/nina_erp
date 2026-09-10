using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/payments")]
public sealed class PaymentsController(IPaymentService payments):ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value); private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub")!);
    public sealed record RefundRequest(decimal Amount,string IdempotencyKey);
    [HttpGet("sale/{saleId:guid}")] public async Task<IActionResult> Sale(Guid saleId,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await payments.GetSaleTransactionsAsync(CompanyId,saleId,ct)));
    [HttpPost("{id:guid}/refund"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Refund(Guid id,RefundRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await payments.RefundAsync(CompanyId,UserId,id,r.Amount,r.IdempotencyKey,ct)));
}
