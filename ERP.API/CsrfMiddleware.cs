using System.Security.Cryptography;
using System.Text;

namespace ERP.API;

public sealed class CsrfMiddleware
{
    public const string CookieName = "NinaERP.Csrf";
    public const string HeaderName = "X-CSRF-TOKEN";
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            var cookie = context.Request.Cookies[CookieName];
            var header = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(cookie) || string.IsNullOrWhiteSpace(header) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(header)))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Token CSRF ausente ou inválido." });
                return;
            }
        }

        await _next(context);
    }
}
