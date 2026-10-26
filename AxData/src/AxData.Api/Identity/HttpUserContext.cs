using System.Security.Claims;
using AxData.Api.Errors;
using AxData.Application.Ports;
using AxData.Domain.Errors;

namespace AxData.Api.Identity;

public static class HttpUserContextClaims
{
    public const string TenantId = "tenant_id";
    public const string AdministratorRole = "admin";
    public const string ArmSessionId = "sid";
}

public sealed class HttpUserContext(
    IHttpContextAccessor accessor,
    IAxpertSessionResolver sessionResolver) : IUserContext
{
    private const string DevelopmentAuthenticationType = "ConfiguredDevelopment";

    public async Task<AxUserContext> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new AxDataApiException(AxDataErrors.AuthenticationRequired);
        }

        if (string.Equals(
                principal.Identity.AuthenticationType,
                DevelopmentAuthenticationType,
                StringComparison.Ordinal))
        {
            return DevelopmentContext(principal);
        }

        var tokenUserName = principal.FindFirstValue(ClaimTypes.Name);
        var sessionId = principal.FindFirstValue(HttpUserContextClaims.ArmSessionId);
        var nameIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(tokenUserName) ||
            string.IsNullOrWhiteSpace(sessionId) ||
            !string.Equals(sessionId, nameIdentifier, StringComparison.Ordinal))
        {
            throw new AxDataApiException(AxDataErrors.AuthenticationRequired);
        }

        var session = await sessionResolver.ResolveAsync(
            new AxpertSessionRequest(sessionId, tokenUserName),
            cancellationToken).ConfigureAwait(false);
        if (session is null ||
            !string.Equals(session.SessionId, sessionId, StringComparison.Ordinal) ||
            !string.Equals(session.UserName, tokenUserName, StringComparison.Ordinal))
        {
            throw new AxDataApiException(AxDataErrors.AuthenticationRequired);
        }

        return new AxUserContext(
            session.TenantId,
            session.UserName,
            session.Roles,
            IsAdministrator: false);
    }

    private static AxUserContext DevelopmentContext(ClaimsPrincipal principal)
    {
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

        return user;
    }
}

public sealed class UnavailableAxpertSessionResolver : IAxpertSessionResolver
{
    public Task<AxpertSessionContext?> ResolveAsync(
        AxpertSessionRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<AxpertSessionContext?>(null);
    }
}
