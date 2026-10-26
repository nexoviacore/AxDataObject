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
    Task<AxpertSessionContext?> ResolveAsync(
        AxpertSessionRequest request,
        CancellationToken cancellationToken);
}
