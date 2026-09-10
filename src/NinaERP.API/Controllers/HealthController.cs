using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinaERP.Infrastructure.Persistence;

namespace NinaERP.API.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var databaseOk = await _db.Database.CanConnectAsync(ct);
        var payload = new
        {
            status = databaseOk ? "healthy" : "degraded",
            database = databaseOk ? "connected" : "unavailable",
            version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            utc = DateTime.UtcNow
        };
        return databaseOk ? Ok(payload) : StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
    }
}
