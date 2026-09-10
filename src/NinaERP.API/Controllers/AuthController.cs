using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NinaERP.Application.Common.Interfaces;
using NinaERP.Contracts.Responses;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public const string SessionCookieName = "ninaerp_session";

    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUnitOfWork uow,
        IJwtTokenGenerator jwt,
        IConfiguration configuration)
    {
        _uow = uow;
        _jwt = jwt;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetForLoginAsync(request.Username, request.CompanyCnpj, ct);
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Usuário ou senha inválidos."));

        var token = _jwt.GenerateToken(user);
        Response.Cookies.Append(SessionCookieName, token, BuildSessionCookieOptions());

        return Ok(ApiResponse<AuthResponse>.Ok(
            new AuthResponse(user.Username, user.FullName, user.Role.ToString(), user.CompanyId)));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<AuthResponse>> Me()
    {
        var username = User.FindFirstValue("username") ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var fullName = User.FindFirstValue(ClaimTypes.Name) ?? username;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var companyIdRaw = User.FindFirstValue("companyId");
        if (!Guid.TryParse(companyIdRaw, out var companyId))
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Sessão inválida."));

        return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(username, fullName, role, companyId)));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookie(),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
        return NoContent();
    }

    private CookieOptions BuildSessionCookieOptions()
    {
        var expirationMinutes = Math.Clamp(_configuration.GetValue("Jwt:ExpirationMinutes", 480), 5, 1440);
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookie(),
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(expirationMinutes),
            IsEssential = true
        };
    }

    private bool ShouldUseSecureCookie()
    {
        // Uma conexão HTTPS nunca deve emitir o cookie de sessão sem o atributo Secure.
        // Em homologação HTTP, Auth:SecureCookie pode permanecer false; produção é
        // bloqueada pelo readiness enquanto TLS/Secure não estiverem ativos.
        return Request.IsHttps || (_configuration.GetValue<bool?>("Auth:SecureCookie") ?? false);
    }
}

public record LoginRequest(string CompanyCnpj, string Username, string Password);
