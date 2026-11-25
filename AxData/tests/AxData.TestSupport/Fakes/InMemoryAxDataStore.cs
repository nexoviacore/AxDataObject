using AxData.Application.Ports;
using AxData.Domain.Model;

namespace AxData.TestSupport.Fakes;

public sealed class InMemoryAxDataStore : IAxDataStore
{
    private readonly Dictionary<(string TenantId, string ObjectId), StoredState> _states = new();
    private readonly HashSet<(string TenantId, string ObjectId)> _closed = [];

    public int WriteCount { get; private set; }

    public int LoadCount { get; private set; }

    public string? LastLockToken { get; private set; }

    public Task<AxDataState?> LoadAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LoadCount++;
        _states.TryGetValue((Normalize(tenantId), Normalize(objectId)), out var stored);
        return Task.FromResult(stored?.State);
    }

    public Task<bool> IsClosedAsync(string tenantId, string objectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_closed.Contains((Normalize(tenantId), Normalize(objectId))));
    }

    public void MarkClosed(string tenantId, string objectId) =>
        _closed.Add((Normalize(tenantId), Normalize(objectId)));

    public Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        string? lockToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WriteCount++;
        LastLockToken = lockToken;
        var key = (Normalize(state.TenantId), Normalize(state.ObjectId));

        if (_states.ContainsKey(key) && lockToken is null)
        {
            return Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.Conflict));
        }
        else if (!_states.ContainsKey(key) && lockToken is not null)
        {
            return Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.Conflict));
        }

        _states[key] = new StoredState(state);
        return Task.FromResult(new AxDataStoreWriteResult(AxDataStoreWriteStatus.Stored));
    }

    private static string Normalize(string value) => value.ToUpperInvariant();

    private sealed record StoredState(AxDataState State);
}

