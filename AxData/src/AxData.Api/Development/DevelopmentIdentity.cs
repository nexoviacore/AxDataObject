using System.Security.Claims;

namespace AxData.Api.Development;

public sealed record DevelopmentIdentityOptions(
    string TenantId,
    string UserName,
    IReadOnlyList<string> Roles);

public sealed class DevelopmentIdentityMiddleware(
    RequestDelegate next,
    DevelopmentIdentityOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        var claims = new List<Claim>
        {
            new("tenant_id", options.TenantId),
            new(ClaimTypes.NameIdentifier, options.UserName)
        };
        claims.AddRange(options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ConfiguredDevelopment"));
        return next(context);
    }
}
