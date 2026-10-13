using System.Security.Claims;
using System.Text;
using AxData.Api.Errors;
using AxData.Api.Identity;
using Microsoft.AspNetCore.Http;

namespace AxData.ContractTests;

public sealed class HttpUserContextTests
{
    [Fact]
    public async Task Context_uses_authenticated_claims_and_ignores_body_identity_fields()
    {
        var context = CreateContext(
            new Claim(HttpUserContextClaims.TenantId, "tenant-a"),
            new Claim(ClaimTypes.NameIdentifier, "user-a"),
            new Claim(ClaimTypes.Role, "ADMIN"),
            new Claim(ClaimTypes.Role, "approver"));
        context.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes("{\"tenantId\":\"tenant-evil\",\"owner\":\"user-evil\"}"));

        var provider = new HttpUserContext(new HttpContextAccessor { HttpContext = context });
        var user = await provider.GetCurrentAsync(CancellationToken.None);

        Assert.Equal("tenant-a", user.TenantId);
        Assert.Equal("user-a", user.UserName);
        Assert.True(user.IsAdministrator);
        Assert.Contains("approver", user.Roles);
    }

    [Fact]
    public async Task Unauthenticated_principal_is_rejected()
    {
        var context = new DefaultHttpContext();
        var provider = new HttpUserContext(new HttpContextAccessor { HttpContext = context });

        var exception = await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));

        Assert.Equal("AXD-AUTH-001", exception.Error.Code);
    }

    [Fact]
    public async Task Missing_tenant_claim_is_rejected()
    {
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, "user-a"));
        var provider = new HttpUserContext(new HttpContextAccessor { HttpContext = context });

        await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));
    }

    private static DefaultHttpContext CreateContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        return context;
    }
}
