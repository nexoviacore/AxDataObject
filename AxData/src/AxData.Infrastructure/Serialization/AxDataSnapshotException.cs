namespace AxData.Infrastructure.Serialization;

public sealed class AxDataSnapshotException : Exception
{
    public AxDataSnapshotException(string message)
        : base(message)
    {
    }

    public AxDataSnapshotException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
