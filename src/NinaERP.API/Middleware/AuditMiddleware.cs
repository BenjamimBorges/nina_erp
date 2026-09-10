using System.Security.Claims;
using NinaERP.Domain.Entities;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.API.Middleware;

public sealed class AuditMiddleware
{
    private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (!AuditedMethods.Contains(context.Request.Method) || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        context.Response.OnCompleted(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AuditLogs.Add(new AuditLog
                {
                    CompanyId = ParseGuid(context.User.FindFirstValue("companyId")),
                    UserId = ParseGuid(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")),
                    Username = context.User.FindFirstValue("username") ?? context.User.Identity?.Name ?? string.Empty,
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value ?? string.Empty,
                    StatusCode = context.Response.StatusCode,
                    RemoteIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 500),
                    CorrelationId = context.TraceIdentifier,
                    OccurredAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao persistir auditoria da requisição {Method} {Path}", context.Request.Method, context.Request.Path);
            }
        });

        await _next(context);
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
