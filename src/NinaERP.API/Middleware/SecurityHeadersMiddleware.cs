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
            if (context.Request.Path.StartsWithSegments("/api"))
                h.TryAdd("Cache-Control", "no-store, max-age=0");
            return Task.CompletedTask;
        });
        await next(context);
    }
}
