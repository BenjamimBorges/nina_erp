namespace NinaERP.API.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h.TryAdd("X-Content-Type-Options", "nosniff");
            h.TryAdd("X-Frame-Options", "SAMEORIGIN");
            h.TryAdd("Referrer-Policy", "no-referrer");
            h.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            h.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            h.TryAdd("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self'; form-action 'self'");
            if (context.Request.IsHttps)
                h.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            if (context.Request.Path.StartsWithSegments("/api"))
                h.TryAdd("Cache-Control", "no-store, max-age=0");
            return Task.CompletedTask;
        });
        await next(context);
    }
}
