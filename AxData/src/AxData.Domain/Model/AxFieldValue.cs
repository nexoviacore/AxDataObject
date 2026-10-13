namespace AxData.Domain.Model;

public sealed class AxFieldValue
{
    public AxFieldValue(object? value, bool isReadOnly = false)
    {
        Value = value;
        OriginalValue = value;
        IsReadOnly = isReadOnly;
    }

    public object? Value { get; private set; }

    public object? OriginalValue { get; private set; }

    public bool IsDirty { get; private set; }

    public bool IsReadOnly { get; }

    public static AxFieldValue Rehydrate(object? value, object? originalValue, bool isDirty, bool isReadOnly)
    {
        if (isDirty != !ValuesEqual(value, originalValue))
        {
            throw new ArgumentException("Dirty state must match the current and original field values.", nameof(isDirty));
        }

        return new AxFieldValue(value, originalValue, isDirty, isReadOnly);
    }

    public bool SetValue(object? value)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("A read-only field cannot be changed.");
        }

        if (ValuesEqual(Value, value))
        {
            return false;
        }

        Value = value;
        IsDirty = !ValuesEqual(OriginalValue, value);
        return true;
    }

    public void AcceptChanges()
    {
        OriginalValue = Value;
        IsDirty = false;
    }

    private AxFieldValue(object? value, object? originalValue, bool isDirty, bool isReadOnly)
    {
        Value = value;
        OriginalValue = originalValue;
        IsDirty = isDirty;
        IsReadOnly = isReadOnly;
    }

    private static bool ValuesEqual(object? left, object? right) =>
        left is byte[] leftBytes && right is byte[] rightBytes
            ? leftBytes.AsSpan().SequenceEqual(rightBytes)
            : Equals(left, right);
}
