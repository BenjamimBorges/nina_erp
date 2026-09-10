using NinaERP.API.Controllers;

namespace NinaERP.API.Middleware;

/// <summary>
/// Defesa complementar para a sessão JWT transportada em cookie HttpOnly.
/// Escritas autenticadas por cookie e o próprio login exigem um header customizado,
/// que formulários HTML cross-site não conseguem enviar. Requisições fetch cross-site
/// com esse header passam por CORS/preflight e só origens autorizadas são aceitas.
/// Webhooks são exceção porque usam autenticação/verificação própria do provedor.
/// </summary>
public sealed class AntiCsrfMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method) || IsWebhookExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        var usesSessionCookie = context.Request.Cookies.ContainsKey(AuthController.SessionCookieName);
        var isLogin = context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase);
        if (!usesSessionCookie && !isLogin)
        {
            // Clientes máquina-a-máquina autenticados por Authorization: Bearer continuam permitidos.
            await next(context);
            return;
        }

        var requestedWith = context.Request.Headers["X-Requested-With"].ToString();
        if (!string.Equals(requestedWith, "XMLHttpRequest", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Requisição bloqueada pela proteção CSRF."
            });
            return;
        }

        await next(context);
    }

    private static bool IsWebhookExempt(PathString path) =>
        path.StartsWithSegments("/api/paymentwebhooks", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/api/payment-webhooks", StringComparison.OrdinalIgnoreCase);
}
