using System.Security.Claims;
using System.Text.Encodings.Web;
using ERP.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ERP.API;

public sealed class CookieSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "NinaSession";
    public const string CookieName = "NinaERP.Session";
    private readonly ISessionService _sessions;

    public CookieSessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISessionService sessions)
        : base(options, logger, encoder) => _sessions = sessions;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var rawToken) || string.IsNullOrWhiteSpace(rawToken))
            return AuthenticateResult.NoResult();

        var user = await _sessions.ValidateAsync(rawToken, Context.RequestAborted);
        if (user is null) return AuthenticateResult.Fail("Sessão inválida ou expirada.");

        var identity = new ClaimsIdentity(Scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        identity.AddClaim(new Claim("companyId", user.CompanyId.ToString()));
        identity.AddClaim(new Claim("fullName", user.FullName));
        identity.AddClaim(new Claim("companyName", user.Company?.Name ?? string.Empty));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme));
    }
}
