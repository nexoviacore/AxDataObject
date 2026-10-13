using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed class AxDataState
{
    private static readonly IReadOnlyDictionary<AxDataLifecycleStatus, AxDataLifecycleStatus[]> AllowedTransitions =
        new Dictionary<AxDataLifecycleStatus, AxDataLifecycleStatus[]>
        {
            [AxDataLifecycleStatus.Active] = [AxDataLifecycleStatus.PendingApproval, AxDataLifecycleStatus.Saved, AxDataLifecycleStatus.Expired, AxDataLifecycleStatus.Closed],
            [AxDataLifecycleStatus.PendingApproval] = [AxDataLifecycleStatus.ApprovedAwaitingSave, AxDataLifecycleStatus.Saved, AxDataLifecycleStatus.Rejected, AxDataLifecycleStatus.Recalled, AxDataLifecycleStatus.Expired],
            [AxDataLifecycleStatus.ApprovedAwaitingSave] = [AxDataLifecycleStatus.Saved, AxDataLifecycleStatus.Expired, AxDataLifecycleStatus.Closed],
            [AxDataLifecycleStatus.Rejected] = [AxDataLifecycleStatus.Active, AxDataLifecycleStatus.Closed, AxDataLifecycleStatus.Expired],
            [AxDataLifecycleStatus.Recalled] = [AxDataLifecycleStatus.Active, AxDataLifecycleStatus.Closed, AxDataLifecycleStatus.Expired],
            [AxDataLifecycleStatus.Saved] = [AxDataLifecycleStatus.Closed],
            [AxDataLifecycleStatus.Expired] = [],
            [AxDataLifecycleStatus.Closed] = []
        };

    public AxDataState(
        string objectId,
        string tenantId,
        string transId,
        string ownerUserName,
        string initiatorUserName,
        string structDefVersion,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatorUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(structDefVersion);

        ObjectId = objectId;
        TenantId = tenantId;
        TransId = transId;
        OwnerUserName = ownerUserName;
        InitiatorUserName = initiatorUserName;
        StructDefVersion = structDefVersion;
        CreatedAtUtc = createdAtUtc;
        LastAccessedAtUtc = createdAtUtc;
    }

    public string ObjectId { get; }
    public string TenantId { get; }
    public string TransId { get; }
    public string? KeyField { get; set; }
    public string? KeyValue { get; set; }
    public long? RecordId { get; set; }
    public string OwnerUserName { get; }
    public string InitiatorUserName { get; }
    public long Version { get; private set; } = 1;
    public string StructDefVersion { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset LastAccessedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public AxDataLifecycleStatus Status { get; private set; } = AxDataLifecycleStatus.Active;
    public Dictionary<string, AxDataContainer> DataContainers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public PegActiveState? Workflow { get; set; }
    public List<AxFileReference> Files { get; } = [];
    public bool AxOpen { get; set; }

    public static AxDataState Rehydrate(
        string objectId,
        string tenantId,
        string transId,
        string? keyField,
        string? keyValue,
        long? recordId,
        string ownerUserName,
        string initiatorUserName,
        long version,
        string structDefVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset lastAccessedAtUtc,
        DateTimeOffset? expiresAtUtc,
        AxDataLifecycleStatus status,
        IEnumerable<AxDataContainer> dataContainers,
        PegActiveState? workflow,
        IEnumerable<AxFileReference> files,
        bool axOpen)
    {
        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be positive.");
        }

        var state = new AxDataState(
            objectId,
            tenantId,
            transId,
            ownerUserName,
            initiatorUserName,
            structDefVersion,
            createdAtUtc)
        {
            KeyField = keyField,
            KeyValue = keyValue,
            RecordId = recordId,
            Version = version,
            LastAccessedAtUtc = lastAccessedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            Status = status,
            Workflow = workflow,
            AxOpen = axOpen
        };

        foreach (var container in dataContainers)
        {
            if (!state.DataContainers.TryAdd(container.Name, container))
            {
                throw new ArgumentException($"Data container '{container.Name}' occurs more than once.", nameof(dataContainers));
            }
        }

        state.Files.AddRange(files);
        return state;
    }

    public bool TransitionTo(AxDataLifecycleStatus target)
    {
        if (target == Status)
        {
            return false;
        }

        if (!AllowedTransitions[Status].Contains(target))
        {
            throw new InvalidOperationException($"Lifecycle transition from {Status} to {target} is not allowed.");
        }

        Status = target;
        Version++;
        return true;
    }

    public long RegisterMutation()
    {
        if (Status is AxDataLifecycleStatus.Expired or AxDataLifecycleStatus.Closed)
        {
            throw new InvalidOperationException($"An object in {Status} state cannot be mutated.");
        }

        return ++Version;
    }
}
