namespace AxData.Infrastructure.Serialization;

public sealed class AxDataSnapshotOptions
{
    public const int DefaultMaximumSerializedBytes = 32 * 1024 * 1024;
    public const int DefaultMaximumGridRows = 10_000;
    public const int DefaultMaximumFieldInstances = 350_000;

    public int MaximumSerializedBytes { get; init; } = DefaultMaximumSerializedBytes;

    public int MaximumGridRows { get; init; } = DefaultMaximumGridRows;

    public int MaximumFieldInstances { get; init; } = DefaultMaximumFieldInstances;
}
