using System.Net;
using ERP.API;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ERP.Tests;

public sealed class LoginRateLimitPolicyTests
{
    [Fact]
    public void Uses_fixed_short_window_without_identity_lockout()
    {
        Assert.Equal("login", LoginRateLimitPolicy.Name);
        Assert.Equal(5, LoginRateLimitPolicy.PermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), LoginRateLimitPolicy.Window);
    }

    [Fact]
    public void Partitions_by_remote_ip_only()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        Assert.Equal("192.0.2.10", LoginRateLimitPolicy.PartitionKey(context));
    }
}
