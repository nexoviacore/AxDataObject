namespace AxData.Domain.Model;

public sealed class AxFieldValue
{
    public AxFieldValue(object? value, bool isReadOnly = false, long? sourceId = null)
    {
        EnsureSourceId(sourceId, nameof(sourceId));
        Value = value;
        OriginalValue = value;
        SourceId = sourceId;
        OriginalSourceId = sourceId;
        IsReadOnly = isReadOnly;
    }

    public object? Value { get; private set; }

    public object? OriginalValue { get; private set; }

    public long? SourceId { get; private set; }

    public long? OriginalSourceId { get; private set; }

    public bool IsDirty { get; private set; }

    public bool IsReadOnly { get; }

    public static AxFieldValue Rehydrate(
        object? value,
        object? originalValue,
        bool isDirty,
        bool isReadOnly,
        long? sourceId = null,
        long? originalSourceId = null)
    {
        EnsureSourceId(sourceId, nameof(sourceId));
        EnsureSourceId(originalSourceId, nameof(originalSourceId));
        if (isDirty != (!ValuesEqual(value, originalValue) || sourceId != originalSourceId))
        {
            throw new ArgumentException(
                "Dirty state must match the current and original field value/source-id pairs.",
                nameof(isDirty));
        }

        return new AxFieldValue(value, originalValue, sourceId, originalSourceId, isDirty, isReadOnly);
    }

    public bool SetValue(object? value)
        => SetValue(value, SourceId);

    public bool SetValue(object? value, long? sourceId)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("A read-only field cannot be changed.");
        }

        EnsureSourceId(sourceId, nameof(sourceId));
        if (ValuesEqual(Value, value) && SourceId == sourceId)
        {
            return false;
        }

        Value = value;
        SourceId = sourceId;
        IsDirty = !ValuesEqual(OriginalValue, value) || OriginalSourceId != sourceId;
        return true;
    }

    public void AcceptChanges()
    {
        OriginalValue = Value;
        OriginalSourceId = SourceId;
        IsDirty = false;
    }

    private AxFieldValue(
        object? value,
        object? originalValue,
        long? sourceId,
        long? originalSourceId,
        bool isDirty,
        bool isReadOnly)
    {
        Value = value;
        OriginalValue = originalValue;
        SourceId = sourceId;
        OriginalSourceId = originalSourceId;
        IsDirty = isDirty;
        IsReadOnly = isReadOnly;
    }

    private static void EnsureSourceId(long? sourceId, string parameterName)
    {
        if (sourceId is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "A source ID must be positive when present.");
        }
    }

    private static bool ValuesEqual(object? left, object? right) =>
        left is byte[] leftBytes && right is byte[] rightBytes
            ? leftBytes.AsSpan().SequenceEqual(rightBytes)
            : Equals(left, right);
}
