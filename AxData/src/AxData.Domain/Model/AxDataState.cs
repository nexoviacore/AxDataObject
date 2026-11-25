using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed class AxDataState
{
    private static readonly IReadOnlyDictionary<AxDataLifecycleStatus, AxDataLifecycleStatus[]> AllowedTransitions =
        new Dictionary<AxDataLifecycleStatus, AxDataLifecycleStatus[]>
        {
            [AxDataLifecycleStatus.Active] = [AxDataLifecycleStatus.PendingApproval, AxDataLifecycleStatus.Saved, AxDataLifecycleStatus.Expired, AxDataLifecycleStatus.Closed],
            [AxDataLifecycleStatus.Saving] = [],
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
        string initiatorUserName,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatorUserName);

        ObjectId = objectId;
        TenantId = tenantId;
        TransId = transId;
        InitiatorUserName = initiatorUserName;
        CreatedAtUtc = createdAtUtc;
        LastAccessedAtUtc = createdAtUtc;
    }

    public string ObjectId { get; }
    public string TenantId { get; }
    public string TransId { get; }
    public long? RecordId { get; set; }
    public string InitiatorUserName { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset LastAccessedAtUtc { get; set; }
    public AxDataLifecycleStatus Status { get; private set; } = AxDataLifecycleStatus.Active;
    public Dictionary<string, AxDataContainer> DataContainers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public PegActiveState? Workflow { get; set; }
    public List<AxFileReference> Files { get; } = [];
    public bool AxOpen { get; set; }

    /// <summary>Recreates persisted state while validating identity, lifecycle, and contained domain invariants.</summary>
    public static AxDataState Rehydrate(
        string objectId,
        string tenantId,
        string transId,
        long? recordId,
        string initiatorUserName,
        DateTimeOffset createdAtUtc,
        DateTimeOffset lastAccessedAtUtc,
        AxDataLifecycleStatus status,
        IEnumerable<AxDataContainer> dataContainers,
        PegActiveState? workflow,
        IEnumerable<AxFileReference> files,
        bool axOpen)
    {
        var state = new AxDataState(
            objectId,
            tenantId,
            transId,
            initiatorUserName,
            createdAtUtc)
        {
            RecordId = recordId,
            LastAccessedAtUtc = lastAccessedAtUtc,
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

    /// <summary>Applies a valid lifecycle transition.</summary>
    /// <returns><see langword="true"/> when state changed; otherwise <see langword="false"/>.</returns>
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
        return true;
    }

    /// <summary>Claims the object for a persistent save operation.</summary>
    public void BeginSaving()
    {
        if (Status != AxDataLifecycleStatus.Active)
        {
            throw new InvalidOperationException($"An object in {Status} state cannot be claimed for Save.");
        }

        Status = AxDataLifecycleStatus.Saving;
    }

    /// <summary>Returns a failed save claim to active state.</summary>
    public void RollbackSaving()
    {
        if (Status != AxDataLifecycleStatus.Saving)
        {
            throw new InvalidOperationException($"An object in {Status} state has no Save claim to release.");
        }

        Status = AxDataLifecycleStatus.Active;
    }

    /// <summary>Completes a successful save by reactivating or closing the in-memory object.</summary>
    /// <param name="retainDataObject">Whether the saved object remains available for further operations.</param>
    public void FinalizeSaving(bool retainDataObject)
    {
        if (Status != AxDataLifecycleStatus.Saving)
        {
            throw new InvalidOperationException($"An object in {Status} state has no Save claim to finalize.");
        }

        Status = retainDataObject ? AxDataLifecycleStatus.Active : AxDataLifecycleStatus.Closed;
    }

    /// <summary>Advances mutation metadata after an atomic state change.</summary>
    public void RegisterMutation()
    {
        if (Status is AxDataLifecycleStatus.Saving or AxDataLifecycleStatus.Saved or AxDataLifecycleStatus.Expired or AxDataLifecycleStatus.Closed)
        {
            throw new InvalidOperationException($"An object in {Status} state cannot be mutated.");
        }

    }
}
