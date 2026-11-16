using Microsoft.AspNetCore.Http;

namespace ERP.API;

public static class RegistrationRateLimitPolicy
{
    public const string Name = "registration";
    public const int PermitLimit = 3;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public static string PartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
