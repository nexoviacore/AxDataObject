namespace AxData.Application.Ports;

public interface IAxDataLock
{
    Task<IAxDataLockLease?> TryAcquireAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken);
}

public interface IAxDataLockLease : IAsyncDisposable;
