using AxData.Application.Ports;
using AxData.Domain.Model;

namespace AxData.TestSupport.Fakes;

public sealed class InMemoryAxDataStore : IAxDataStore
{
    private readonly Dictionary<(string TenantId, string ObjectId), StoredState> _states = new();

    public int WriteCount { get; private set; }

    public long? LastExpectedVersion { get; private set; }

    public Task<AxDataState?> LoadAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _states.TryGetValue((Normalize(tenantId), Normalize(objectId)), out var stored);
        return Task.FromResult(stored?.State);
    }

    public Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WriteCount++;
        LastExpectedVersion = expectedVersion;
        var key = (Normalize(state.TenantId), Normalize(state.ObjectId));

        if (_states.TryGetValue(key, out var current))
        {
            if (expectedVersion is null || current.Version != expectedVersion)
            {
                return Task.FromResult(new AxDataStoreWriteResult(
                    AxDataStoreWriteStatus.StaleVersion,
                    current.Version));
            }
        }
        else if (expectedVersion is not null)
        {
            return Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.StaleVersion));
        }

        _states[key] = new StoredState(state, state.Version);
        return Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.Stored, state.Version));
    }

    private static string Normalize(string value) => value.ToUpperInvariant();

    private sealed record StoredState(AxDataState State, long Version);
}

