using AxData.Domain.Model;

namespace AxData.Application.Ports;

public interface IAxDataStore
{
    /// <summary>Loads an object using its tenant-scoped identity.</summary>
    /// <returns>The state, or <see langword="null"/> when no tenant-scoped object exists.</returns>
    Task<AxDataState?> LoadAsync(string tenantId, string objectId, CancellationToken cancellationToken);

    /// <summary>Determines whether a durable closed marker exists for the tenant-scoped object.</summary>
    Task<bool> IsClosedAsync(string tenantId, string objectId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    /// <summary>Creates a new state or atomically writes an existing state owned by a lock token.</summary>
    /// <param name="lockToken"><see langword="null"/> for create-only mode; otherwise the mutation lease token.</param>
    /// <returns><see cref="AxDataStoreWriteStatus.Conflict"/> when creation or token ownership verification fails.</returns>
    Task<AxDataStoreWriteResult> WriteAsync(
        AxDataState state,
        string? lockToken,
        CancellationToken cancellationToken);
}

public sealed record AxDataStoreWriteResult(AxDataStoreWriteStatus Status);

public enum AxDataStoreWriteStatus
{
    Stored,
    Conflict
}

