using AxData.Application.Ports;

namespace AxData.TestSupport.Fakes;

public sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

public sealed class FakeObjectIdGenerator(params string[] objectIds) : IObjectIdGenerator
{
    private readonly Queue<string> _objectIds = new(objectIds);

    public string NewObjectId() =>
        _objectIds.Count > 0
            ? _objectIds.Dequeue()
            : throw new InvalidOperationException("No scripted ObjectId remains.");
}

public sealed class FakeRowIdGenerator(params string[] rowIds) : IRowIdGenerator
{
    private readonly Queue<string> _rowIds = new(rowIds);
    private int _next;

    public string NewRowId() =>
        _rowIds.Count > 0
            ? _rowIds.Dequeue()
            : $"row_fake_{++_next}";
}

public sealed class FakeUserContext(AxUserContext current) : IUserContext
{
    public Task<AxUserContext> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(current);
    }
}

public sealed class FakeAxDataLock : IAxDataLock
{
    public bool CanAcquire { get; set; } = true;

    public int AcquireCount { get; private set; }

    public Task<IAxDataLockLease?> TryAcquireAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AcquireCount++;
        return Task.FromResult<IAxDataLockLease?>(CanAcquire ? new Lease() : null);
    }

    private sealed class Lease : IAxDataLockLease
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

