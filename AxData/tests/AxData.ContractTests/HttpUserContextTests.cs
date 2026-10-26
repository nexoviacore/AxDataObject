using System.Security.Claims;
using System.Text;
using AxData.Api.Errors;
using AxData.Api.Identity;
using AxData.Application.Ports;
using Microsoft.AspNetCore.Http;

namespace AxData.ContractTests;

public sealed class HttpUserContextTests
{
    [Fact]
    public async Task Context_uses_authenticated_claims_and_ignores_body_identity_fields()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.Name, "user-a"),
            new Claim(HttpUserContextClaims.ArmSessionId, "ARM-app-session"),
            new Claim(ClaimTypes.NameIdentifier, "ARM-app-session"),
            new Claim(ClaimTypes.Role, "attacker-role"));
        context.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes("{\"tenantId\":\"tenant-evil\",\"owner\":\"user-evil\"}"));

        var provider = Provider(
            context,
            new AxpertSessionContext(
                "ARM-app-session",
                "tenant-a",
                "user-a",
                new HashSet<string>(["approver"], StringComparer.OrdinalIgnoreCase),
                3));
        var user = await provider.GetCurrentAsync(CancellationToken.None);

        Assert.Equal("tenant-a", user.TenantId);
        Assert.Equal("user-a", user.UserName);
        Assert.False(user.IsAdministrator);
        Assert.Contains("approver", user.Roles);
        Assert.DoesNotContain("attacker-role", user.Roles);
    }

    [Fact]
    public async Task Unauthenticated_principal_is_rejected()
    {
        var context = new DefaultHttpContext();
        var provider = Provider(context, null);

        var exception = await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));

        Assert.Equal("AXD-AUTH-001", exception.Error.Code);
    }

    [Fact]
    public async Task Cleared_session_is_rejected()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.Name, "user-a"),
            new Claim(HttpUserContextClaims.ArmSessionId, "ARM-app-session"),
            new Claim(ClaimTypes.NameIdentifier, "ARM-app-session"));
        var provider = Provider(context, null);

        await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Disagreeing_token_session_claims_are_rejected()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.Name, "user-a"),
            new Claim(HttpUserContextClaims.ArmSessionId, "ARM-app-one"),
            new Claim(ClaimTypes.NameIdentifier, "ARM-app-two"));
        var provider = Provider(context, null);

        var exception = await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));

        Assert.Equal("AXD-AUTH-001", exception.Error.Code);
    }

    [Fact]
    public async Task Token_and_live_session_username_disagreement_is_rejected()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.Name, "token-user"),
            new Claim(HttpUserContextClaims.ArmSessionId, "ARM-app-session"),
            new Claim(ClaimTypes.NameIdentifier, "ARM-app-session"));
        var provider = Provider(
            context,
            new AxpertSessionContext(
                "ARM-app-session",
                "tenant-a",
                "session-user",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                0));

        var exception = await Assert.ThrowsAsync<AxDataApiException>(
            () => provider.GetCurrentAsync(CancellationToken.None));

        Assert.Equal("AXD-AUTH-001", exception.Error.Code);
    }

    private static HttpUserContext Provider(
        DefaultHttpContext context,
        AxpertSessionContext? session) =>
        new(
            new HttpContextAccessor { HttpContext = context },
            new StubSessionResolver(session));

    private static DefaultHttpContext CreateContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        return context;
    }

    private sealed class StubSessionResolver(AxpertSessionContext? session) : IAxpertSessionResolver
    {
        public Task<AxpertSessionContext?> ResolveAsync(
            AxpertSessionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(session);
        }
    }
}
