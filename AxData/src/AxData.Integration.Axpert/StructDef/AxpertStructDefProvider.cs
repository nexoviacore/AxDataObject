using System.Collections.Concurrent;
using AxData.Application.Ports.Axpert;

namespace AxData.Integration.Axpert.StructDef;

public sealed class AxpertStructDefProvider(IAxpertStructDefSource source) : IStructDefProvider
{
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<StructDefHandle?>>> _cache = new();

    public async Task<StructDefHandle?> GetAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(tenantId, transId);
        var snapshot = await LoadCurrentAsync(tenantId, transId, cancellationToken).ConfigureAwait(false);
        return snapshot is null
            ? null
            : await GetOrCreateAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StructDefHandle?> GetVersionAsync(
        string tenantId,
        string transId,
        string version,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(tenantId, transId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await LoadCurrentAsync(tenantId, transId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null || !Matches(snapshot.ContentHash, version))
        {
            return null;
        }

        return await GetOrCreateAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private async Task<StructDefHandle?> GetOrCreateAsync(
        AxpertStructDefSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var key = new CacheKey(snapshot.TenantId, snapshot.TransId, snapshot.ContentHash);
        var pending = _cache.GetOrAdd(
            key,
            _ => new Lazy<Task<StructDefHandle?>>(
                () => Task.FromResult<StructDefHandle?>(Project(snapshot)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await pending.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _cache.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<StructDefHandle?>>>(key, pending));
            throw;
        }
    }

    private async Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        var snapshot = await source.LoadCurrentAsync(
            tenantId,
            transId,
            cancellationToken).ConfigureAwait(false);

        if (snapshot is null ||
            !Matches(snapshot.TenantId, tenantId) ||
            !Matches(snapshot.TransId, transId) ||
            !IsSha256(snapshot.ContentHash))
        {
            return null;
        }

        return snapshot;
    }

    private static StructDefHandle Project(AxpertStructDefSnapshot snapshot) =>
        new(
            snapshot.TenantId,
            snapshot.TransId,
            snapshot.ContentHash,
            snapshot.DataContainers.ToArray(),
            snapshot.Dependencies.ToArray(),
            snapshot.FillGrids.ToArray(),
            new StructDefRevisionInfo(
                snapshot.ContentHash,
                snapshot.UpdatedOn,
                snapshot.LegacyVersionNo,
                snapshot.RunTimeStruct,
                snapshot.RunTimeMode));

    private static void ValidateIdentity(string tenantId, string transId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);
    }

    private static bool Matches(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private readonly record struct CacheKey
    {
        public CacheKey(string tenantId, string transId, string version)
        {
            TenantId = Normalize(tenantId);
            TransId = Normalize(transId);
            Version = Normalize(version);
        }

        public string TenantId { get; }
        public string TransId { get; }
        public string Version { get; }

        private static string Normalize(string value) => value.ToUpperInvariant();
    }
}
