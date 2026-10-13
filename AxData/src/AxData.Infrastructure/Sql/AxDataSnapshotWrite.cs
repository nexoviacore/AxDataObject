using AxData.Domain.Model;

namespace AxData.Infrastructure.Sql;

public sealed record AxDataSnapshotWrite(
    string EventId,
    AxDataState State,
    DateTimeOffset PersistedAtUtc);

public enum AxDataSnapshotUpsertStatus
{
    Stored,
    Ignored
}
