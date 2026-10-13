namespace AxData.Application.Ports;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IObjectIdGenerator
{
    string NewObjectId();
}

public interface IRowIdGenerator
{
    string NewRowId();
}
