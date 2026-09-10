using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Domain.Enums;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/fiscal")]
public sealed class FiscalController(IFiscalService fiscal):ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value);
    [HttpGet("configuration")] public async Task<IActionResult> Configuration(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.GetConfigurationAsync(CompanyId,ct)));
    [HttpPut("configuration"),Authorize(Policy="AdminOnly")] public async Task<IActionResult> SaveConfiguration([FromBody]FiscalConfigurationDto r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.SaveConfigurationAsync(CompanyId,r with { Id=Guid.Empty },ct)));
    public sealed record IssueRequest(Guid SaleId,InvoiceModel Model,string IdempotencyKey,bool AllowContingency=false); public sealed record CancelRequest(string Reason,string IdempotencyKey); public sealed record CorrectionRequest(string Correction,string IdempotencyKey); public sealed record VoidNumbersRequest(InvoiceModel Model,int Series,int FromNumber,int ToNumber,string Reason,string IdempotencyKey); public sealed record GuideRequest(string TaxType,int ReferenceYear,int ReferenceMonth,DateTime DueDate,decimal PrincipalAmount,string? RevenueCode,string? DigitableLine,string? DocumentPath,Guid? AccountPayableId,string? OriginDescription); public sealed record GuidePaidRequest(string ProofPath);
    [HttpGet("readiness")] public async Task<IActionResult> Readiness(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.GetReadinessAsync(CompanyId,ct)));
    [HttpGet("dashboard")] public async Task<IActionResult> Dashboard(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.GetDashboardAsync(CompanyId,ct)));
    [HttpGet("documents")] public async Task<IActionResult> Documents([FromQuery]DateTime? from,[FromQuery]DateTime? to,CancellationToken ct){var end=to??DateTime.UtcNow;var start=from??end.AddDays(-90);return Ok(ApiResponse<object>.Ok(await fiscal.GetInvoicesAsync(CompanyId,start,end,ct)));}
    [HttpPost("documents"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Issue(IssueRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.IssueAsync(CompanyId,r.SaleId,r.Model,r.IdempotencyKey,r.AllowContingency,ct)));
    [HttpPost("documents/{id:guid}/query"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Query(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.QueryAsync(CompanyId,id,ct)));
    [HttpPost("documents/{id:guid}/cancel"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Cancel(Guid id,CancelRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.CancelAsync(CompanyId,id,r.Reason,r.IdempotencyKey,ct)));
    [HttpPost("documents/{id:guid}/correction-letter"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Correction(Guid id,CorrectionRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.CorrectionLetterAsync(CompanyId,id,r.Correction,r.IdempotencyKey,ct)));
    [HttpGet("voids")] public async Task<IActionResult> Voids(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.GetNumberVoidsAsync(CompanyId,ct)));
    [HttpPost("voids"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> VoidNumbers(VoidNumbersRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.VoidNumbersAsync(CompanyId,r.Model,r.Series,r.FromNumber,r.ToNumber,r.Reason,r.IdempotencyKey,ct)));
    [HttpGet("guides")] public async Task<IActionResult> Guides(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.GetTaxGuidesAsync(CompanyId,ct)));
    [HttpPost("guides"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Guide(GuideRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.CreateTaxGuideAsync(CompanyId,r.TaxType,r.ReferenceYear,r.ReferenceMonth,r.DueDate,r.PrincipalAmount,r.RevenueCode,r.DigitableLine,r.DocumentPath,r.AccountPayableId,r.OriginDescription,ct)));
    [HttpPost("guides/{id:guid}/paid"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> GuidePaid(Guid id,GuidePaidRequest r,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await fiscal.MarkTaxGuidePaidAsync(CompanyId,id,r.ProofPath,ct)));
}
