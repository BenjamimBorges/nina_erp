using System.Net;
using Microsoft.AspNetCore.Http;

namespace ERP.API;

public static class LoginRateLimitPolicy
{
    public const string Name = "login";
    public const int PermitLimit = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static string PartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
