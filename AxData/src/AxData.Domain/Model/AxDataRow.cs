using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed class AxDataRow
{
    public AxDataRow(
        string rowId,
        AxRowState state,
        long? persistedRecordId = null,
        IDictionary<string, AxFieldValue>? fields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowId);

        if (persistedRecordId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(persistedRecordId), "A persisted record ID must be positive.");
        }

        if (state == AxRowState.New && persistedRecordId is not null)
        {
            throw new ArgumentException("A new row cannot already have a persisted record ID.", nameof(persistedRecordId));
        }

        if (state != AxRowState.New && persistedRecordId is null)
        {
            throw new ArgumentException("A persisted row requires a persisted record ID.", nameof(persistedRecordId));
        }

        RowId = rowId;
        State = state;
        PersistedRecordId = persistedRecordId;
        Fields = new Dictionary<string, AxFieldValue>(fields ?? new Dictionary<string, AxFieldValue>(), StringComparer.OrdinalIgnoreCase);
    }

    public string RowId { get; }

    public long? PersistedRecordId { get; private set; }

    public AxRowState State { get; private set; }

    public Dictionary<string, AxFieldValue> Fields { get; }

    public bool TransitionTo(AxRowState target)
    {
        if (target == State)
        {
            return false;
        }

        var allowed = State switch
        {
            AxRowState.Unchanged => target is AxRowState.Edited or AxRowState.Deleted,
            AxRowState.Edited => target is AxRowState.Deleted or AxRowState.Unchanged,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Row transition from {State} to {target} is not allowed.");
        }

        State = target;
        return true;
    }

    public void AssignPersistedRecordId(long recordId)
    {
        if (State != AxRowState.New || PersistedRecordId is not null)
        {
            throw new InvalidOperationException("Only an unpersisted new row can receive a record ID.");
        }

        if (recordId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recordId), "A persisted record ID must be positive.");
        }

        PersistedRecordId = recordId;
        State = AxRowState.Unchanged;
    }
}
