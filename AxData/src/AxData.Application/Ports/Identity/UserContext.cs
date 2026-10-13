namespace AxData.Application.Ports;

public sealed record AxUserContext(
    string TenantId,
    string UserName,
    IReadOnlySet<string> Roles,
    bool IsAdministrator = false);

public interface IUserContext
{
    Task<AxUserContext> GetCurrentAsync(CancellationToken cancellationToken);
}

