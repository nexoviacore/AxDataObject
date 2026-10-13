using AxData.Domain.Model;

namespace AxData.Application.Ports;

public interface IAxDataStore
{
    Task<AxDataState?> LoadAsync(string tenantId, string objectId, CancellationToken cancellationToken);

    Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        long? expectedVersion,
        CancellationToken cancellationToken);
}

public sealed record AxDataStoreWriteResult(AxDataStoreWriteStatus Status, long? CurrentVersion = null);

public enum AxDataStoreWriteStatus
{
    Stored,
    StaleVersion
}

