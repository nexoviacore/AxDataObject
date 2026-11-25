using AxData.Contracts.Snapshots;

namespace AxTstructData;

public interface IAxPutDataObjectStore
{
    Task<DataObjectSnapshotV3?> LoadAsync(string tenantId, string objectId);

    Task<AxPutDataObjectStoreResult> ClaimAsync(
        DataObjectSnapshotV3 savingSnapshot,
        DataObjectSnapshotV3? expectedSnapshot,
        string claimToken,
        bool createOnly);

    Task<AxPutDataObjectStoreResult> ReleaseAsync(AxPutDataObjectContext context);

    Task<AxPutDataObjectStoreResult> FinalizeAsync(
        AxPutDataObjectContext context,
        DataObjectSnapshotV3 finalizedSnapshot,
        bool retainDataObject,
        DateTimeOffset persistedAtUtc);
}

public sealed record AxPutDataObjectStoreResult(AxPutDataObjectStoreStatus Status);

public enum AxPutDataObjectStoreStatus
{
    Claimed,
    Released,
    Finalized,
    ActiveConflict,
    ClaimLost
}
