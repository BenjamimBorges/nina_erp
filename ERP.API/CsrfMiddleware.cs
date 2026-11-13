using System.Security.Cryptography;
using System.Text;

namespace ERP.API;

public sealed class CsrfMiddleware
{
    public const string CookieName = "__Host-NinaERP.Csrf";
    public const string HeaderName = "X-CSRF-TOKEN";
    private readonly RequestDelegate _next;
    private readonly CsrfTokenService _tokens;

    public CsrfMiddleware(RequestDelegate next, CsrfTokenService tokens) { _next = next; _tokens = tokens; }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            var cookie = context.Request.Cookies[CookieName];
            var header = context.Request.Headers[HeaderName].ToString();
            var origin = context.Request.Headers.Origin.ToString();
            var allowedOrigin = context.RequestServices.GetRequiredService<IConfiguration>()["Cors:Origin"];
            if (!string.IsNullOrWhiteSpace(origin) && !string.Equals(origin, allowedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Origem não autorizada." });
                return;
            }
            if (string.IsNullOrWhiteSpace(cookie) || string.IsNullOrWhiteSpace(header) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(header)) ||
                !_tokens.IsValid(cookie))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Token CSRF ausente ou inválido." });
                return;
            }
        }

        await _next(context);
    }
}
