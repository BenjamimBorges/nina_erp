using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/reports")]
public sealed class ReportsController(IReportingService reports):ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value);
    [HttpGet("sales-daily")] public async Task<IActionResult> Sales([FromQuery]DateTime from,[FromQuery]DateTime to,[FromQuery]int limit=365,CancellationToken ct=default)=>Ok(ApiResponse<object>.Ok(await reports.GetDailySalesAsync(CompanyId,from,to,limit,ct)));
    [HttpGet("products")] public async Task<IActionResult> Products([FromQuery]DateTime from,[FromQuery]DateTime to,[FromQuery]int limit=100,CancellationToken ct=default)=>Ok(ApiResponse<object>.Ok(await reports.GetProductSalesAsync(CompanyId,from,to,limit,ct)));
    [HttpGet("cash")] public async Task<IActionResult> Cash([FromQuery]DateTime from,[FromQuery]DateTime to,[FromQuery]int limit=100,CancellationToken ct=default)=>Ok(ApiResponse<object>.Ok(await reports.GetCashAsync(CompanyId,from,to,limit,ct)));
    [HttpGet("financial-summary")] public async Task<IActionResult> Financial(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await reports.GetFinancialSummaryAsync(CompanyId,ct)));
}
