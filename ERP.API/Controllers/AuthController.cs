using ERP.Core.Services;
using ERP.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [EnableRateLimiting(ERP.API.LoginRateLimitPolicy.Name)]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _authService.AuthenticateAsync(request, cancellationToken);
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            Response.Cookies.Append(CookieSessionAuthenticationHandler.CookieName, result.SessionToken!, SessionCookieOptions());
            result.SessionToken = null;
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("csrf")]
        public IActionResult Csrf()
        {
            var token = HttpContext.RequestServices.GetRequiredService<CsrfTokenService>().Create();
            Response.Cookies.Append(CsrfMiddleware.CookieName, token, new CookieOptions
            {
                HttpOnly = false, Secure = true, SameSite = SameSiteMode.Lax,
                IsEssential = true, MaxAge = TimeSpan.FromHours(8), Path = "/"
            });
            return Ok(new { token });
        }

        [Authorize(Policy = "User")]
        [HttpGet("me")]
        public ActionResult<UserDto> Me() => Ok(new UserDto
        {
            Id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            Username = User.Identity?.Name ?? string.Empty,
            FullName = User.FindFirstValue("fullName") ?? string.Empty,
            Role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? string.Empty,
            CompanyName = User.FindFirstValue("companyName") ?? string.Empty
        });

        [Authorize(Policy = "Admin")]
        [HttpGet("admin-check")]
        public IActionResult AdminCheck() => Ok(new { authorized = true });

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromServices] ERP.Core.Services.ISessionService sessions, CancellationToken cancellationToken)
        {
            if (Request.Cookies.TryGetValue(CookieSessionAuthenticationHandler.CookieName, out var token))
                await sessions.RevokeAsync(token, "logout", cancellationToken);
            Response.Cookies.Delete(CookieSessionAuthenticationHandler.CookieName, SessionCookieOptions());
            return NoContent();
        }

        private static CookieOptions SessionCookieOptions() => new()
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            IsEssential = true, MaxAge = TimeSpan.FromHours(8), Path = "/"
        };
    }
}
