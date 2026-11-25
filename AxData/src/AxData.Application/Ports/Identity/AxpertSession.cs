namespace AxData.Application.Ports;

public sealed record AxpertSessionRequest(
    string SessionId,
    string UserName);

public sealed record AxpertSessionContext(
    string SessionId,
    string TenantId,
    string UserName,
    IReadOnlySet<string> Roles,
    int SiteNumber);

public interface IAxpertSessionResolver
{
    /// <summary>Validates an Axpert session and resolves its tenant, user, roles, and site.</summary>
    /// <returns>The trusted session context, or <see langword="null"/> when validation fails.</returns>
    Task<AxpertSessionContext?> ResolveAsync(
        AxpertSessionRequest request,
        CancellationToken cancellationToken);
}
