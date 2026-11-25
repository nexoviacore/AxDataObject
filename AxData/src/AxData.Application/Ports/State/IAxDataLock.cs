namespace AxData.Application.Ports;

public interface IAxDataLock
{
    /// <summary>Attempts to acquire the server-side mutation lease for a tenant-scoped object.</summary>
    /// <returns>An owned lease, or <see langword="null"/> when another operation holds it.</returns>
    Task<IAxDataLockLease?> TryAcquireAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken);
}

public interface IAxDataLockLease : IAsyncDisposable
{
    /// <summary>Gets the ownership token required by atomic state writes.</summary>
    string Token { get; }
}
