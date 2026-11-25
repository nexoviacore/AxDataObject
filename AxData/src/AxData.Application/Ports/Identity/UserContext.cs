namespace AxData.Application.Ports;

public sealed record AxUserContext(
    string TenantId,
    string UserName,
    IReadOnlySet<string> Roles);

public interface IUserContext
{
    /// <summary>Resolves the authenticated user and tenant for the current request.</summary>
    Task<AxUserContext> GetCurrentAsync(CancellationToken cancellationToken);
}

