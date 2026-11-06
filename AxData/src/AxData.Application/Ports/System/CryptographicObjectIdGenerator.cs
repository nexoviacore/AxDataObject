using AxData.Contracts.ObjectIds;

namespace AxData.Application.Ports;

public sealed class CryptographicObjectIdGenerator(IClock clock) : IObjectIdGenerator
{
    public string NewObjectId() => CryptographicUlid.New(clock.UtcNow);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
