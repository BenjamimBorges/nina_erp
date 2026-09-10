using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/assistant")]
public sealed class AssistantController(IAssistantService assistant, IConfiguration configuration) : ControllerBase
{
    private Guid CompanyId => Guid.Parse(User.FindFirst("companyId")!.Value);
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    public sealed record AskRequest(string Question);

    [HttpPost("ask")]
    [EnableRateLimiting("assistant")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        if (!configuration.GetValue("Features:Assistant", false))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail("Assistente está desabilitado neste ambiente."));
        return Ok(ApiResponse<object>.Ok(await assistant.AskAsync(CompanyId, UserId, request.Question, ct)));
    }
}
