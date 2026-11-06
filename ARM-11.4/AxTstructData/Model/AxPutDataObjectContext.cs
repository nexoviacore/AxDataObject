using AxData.Contracts.Snapshots;
using AxData.Contracts.ObjectIds;

namespace AxTstructData;

public sealed class AxPutDataObjectContext
{
    public required string ObjectId { get; init; }
    public required string TenantId { get; init; }
    public required long Version { get; init; }
    public required bool ObjectIdWasSupplied { get; init; }
    public required string OwnerUserName { get; init; }
    public required string InitiatorUserName { get; init; }
    public required string TransId { get; init; }
    public string? KeyField { get; init; }
    public string? KeyValue { get; init; }
    public DataObjectSnapshotV3? Snapshot { get; init; }
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>>? Overlay { get; init; }
    public string? CurrentStructDefVersion { get; set; }
    public bool RuntimeInitialized { get; set; }
    public IReadOnlyList<AxPutRecordIdMapping> CommittedRecordIdMappings { get; set; } = [];
    public string ClaimToken { get; } = Guid.NewGuid().ToString("N");
    public bool IsClaimed { get; set; }
    public bool BusinessCommitted { get; set; }
    public bool FinalizationPending { get; set; }
    public bool? DataObjectRetained { get; set; }
    public long PreviousTtlMilliseconds { get; set; } = -1;
    public DataObjectSnapshotV3? RollbackSnapshot { get; set; }
    public DataObjectSnapshotV3? PendingFinalSnapshot { get; set; }
    public long FinalVersion => checked(Version + 1);

    public long? KnownVersion => BusinessCommitted ? FinalVersion : Snapshot is not null || RuntimeInitialized ? Version : null;

    public bool HasRetainedState => DataObjectRetained ?? (Snapshot is not null || RuntimeInitialized);

    public string GetOrCreateRowId(string dcId, int rowNo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dcId);
        ArgumentOutOfRangeException.ThrowIfLessThan(rowNo, 1);

        DataRowSnapshotV3? storedRow = Snapshot?.DataContainers?
            .SingleOrDefault(container => string.Equals(container.DcId, dcId, StringComparison.OrdinalIgnoreCase))?
            .Rows.ElementAtOrDefault(rowNo - 1);
        if (storedRow is not null)
            return storedRow.RowId;

        string key = $"{dcId.ToUpperInvariant()}:{rowNo}";
        if (!_generatedRowIds.TryGetValue(key, out string? rowId))
        {
            rowId = CryptographicUlid.New(DateTimeOffset.UtcNow);
            _generatedRowIds[key] = rowId;
        }

        return rowId;
    }

    private readonly Dictionary<string, string> _generatedRowIds = new(StringComparer.Ordinal);
}

public sealed record AxPutRecordIdMapping(string DcId, string RowId, long RecordId);

public sealed record AxPutRecordIdCapture(
    bool Success,
    IReadOnlyList<AxPutRecordIdMapping> Mappings,
    string? Message = null);

public sealed record AxPutDataObjectCheck(
    bool Success,
    string? Message = null,
    string? ErrorCode = null,
    int HttpStatus = 200,
    long? CurrentVersion = null);

public static class AxPutObjectErrors
{
    public const string InvalidRequest = "AXD-REQ-001";
    public const string AccessDenied = "AXD-AUTH-002";
    public const string StaleVersion = "AXD-CONC-001";
    public const string ActiveConflict = "AXD-CONC-002";
    public const string StructDefChanged = "AXD-CONC-003";
    public const string StateUnavailable = "AXD-INFRA-001";
}
