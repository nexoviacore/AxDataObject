using System.Security.Claims;
using AxData.Api.Errors;
using AxData.Application.Ports;
using AxData.Domain.Errors;

namespace AxData.Api.Identity;

public static class HttpUserContextClaims
{
    public const string TenantId = "tenant_id";
    public const string AdministratorRole = "admin";
}

public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Task<AxUserContext> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new AxDataApiException(AxDataErrors.AuthenticationRequired);
        }

        var tenantId = principal.FindFirstValue(HttpUserContextClaims.TenantId);
        var userName = principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                       principal.FindFirstValue("sub") ??
                       principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userName))
        {
            throw new AxDataApiException(AxDataErrors.AuthenticationRequired);
        }

        IReadOnlySet<string> roles = principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var user = new AxUserContext(
            tenantId,
            userName,
            roles,
            roles.Contains(HttpUserContextClaims.AdministratorRole));

        return Task.FromResult(user);
    }
}
