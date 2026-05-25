using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _jwt;
    public AuthController(IUnitOfWork uow, IJwtTokenGenerator jwt) { _uow = uow; _jwt = jwt; }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByUsernameAsync(request.Username, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Usuário ou senha inválidos."));

        var token = _jwt.GenerateToken(user);
        return Ok(ApiResponse<AuthResponse>.Ok(
            new AuthResponse(token, user.Username, user.FullName, user.Role.ToString(), user.CompanyId)));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> Me()
    {
        var claims = new {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Username = User.FindFirstValue(ClaimTypes.Name),
            Role = User.FindFirstValue(ClaimTypes.Role),
            CompanyId = User.FindFirstValue("companyId")
        };
        return Ok(ApiResponse<object>.Ok(claims));
    }
}

public record LoginRequest(string Username, string Password);
