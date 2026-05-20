using MediatR;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Features.Auth.Commands.Login;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthController(ISender mediator) => _mediator = mediator;

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login realizado com sucesso."));
    }
}
