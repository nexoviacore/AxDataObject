namespace AxData.Application.Ports;

public interface IClock
{
    /// <summary>Gets the current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}

public interface IObjectIdGenerator
{
    /// <summary>Creates a new opaque object reference; the value is never a credential.</summary>
    string NewObjectId();
}

public interface IRowIdGenerator
{
    /// <summary>Creates a new identifier for an in-memory data-container row.</summary>
    string NewRowId();
}
