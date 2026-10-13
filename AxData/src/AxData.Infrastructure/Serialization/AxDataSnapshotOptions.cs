namespace AxData.Infrastructure.Serialization;

public sealed class AxDataSnapshotOptions
{
    public required int MaximumSerializedBytes { get; init; }

    public required int MaximumGridRows { get; init; }
}
