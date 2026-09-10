using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;
namespace NinaERP.API.Controllers;
[Authorize,ApiController,Route("api/tax")]
public sealed class TaxController(ITaxEngineService tax):ControllerBase
{
    private Guid CompanyId=>Guid.Parse(User.FindFirst("companyId")!.Value);
    [HttpGet("profiles")] public async Task<IActionResult> Profiles(CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await tax.GetProfilesAsync(CompanyId,ct)));
    [HttpPost("profiles"),Authorize(Policy="AdminOnly")] public async Task<IActionResult> CreateProfile([FromBody]TaxProfileDto p,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await tax.CreateProfileAsync(CompanyId,p with { Id=Guid.Empty },ct)));
    [HttpPost("profiles/{id:guid}/activate"),Authorize(Policy="AdminOnly")] public async Task<IActionResult> Activate(Guid id,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await tax.ActivateProfileAsync(CompanyId,id,ct)));
    [HttpPost("calculate"),Authorize(Policy="ManagementWrite")] public async Task<IActionResult> Calculate([FromBody]TaxContext c,CancellationToken ct)=>Ok(ApiResponse<object>.Ok(await tax.CalculateAsync(c with { CompanyId=CompanyId },ct)));
}
