using System.Security.Claims;
using AxData.Api.Development;
using Microsoft.AspNetCore.Http;

namespace AxData.ContractTests;

public sealed class M1DevelopmentIdentityTests
{
    [Fact]
    public async Task Configured_identity_replaces_client_supplied_claims()
    {
        var reachedNext = false;
        var middleware = new DevelopmentIdentityMiddleware(
            _ =>
            {
                reachedNext = true;
                return Task.CompletedTask;
            },
            new DevelopmentIdentityOptions("sample-tenant", "owner", ["reviewer"]));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", "attacker-tenant"), new Claim(ClaimTypes.NameIdentifier, "attacker")],
                "Client"))
        };

        await middleware.InvokeAsync(context);

        Assert.True(reachedNext);
        Assert.Equal("sample-tenant", context.User.FindFirstValue("tenant_id"));
        Assert.Equal("owner", context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("reviewer", context.User.FindFirstValue(ClaimTypes.Role));
    }
}
