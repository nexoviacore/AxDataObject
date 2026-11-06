using AxData.Contracts.Snapshots;

namespace AxTstructData;

public interface IAxPutDataObjectStore
{
    Task<DataObjectSnapshotV3?> LoadAsync(string tenantId, string objectId);

    Task<AxPutDataObjectStoreResult> ClaimAsync(
        DataObjectSnapshotV3 savingSnapshot,
        long expectedVersion,
        string claimToken,
        bool createOnly);

    Task<AxPutDataObjectStoreResult> ReleaseAsync(AxPutDataObjectContext context);

    Task<AxPutDataObjectStoreResult> FinalizeAsync(
        AxPutDataObjectContext context,
        DataObjectSnapshotV3 finalizedSnapshot,
        bool retainDataObject,
        DateTimeOffset persistedAtUtc);
}

public sealed record AxPutDataObjectStoreResult(
    AxPutDataObjectStoreStatus Status,
    long? CurrentVersion = null,
    long PreviousTtlMilliseconds = -1);

public enum AxPutDataObjectStoreStatus
{
    Claimed,
    Released,
    Finalized,
    StaleVersion,
    ActiveConflict,
    ClaimLost
}
