namespace AxTstructData;

public sealed class AxSaveDataObject
{
    public string ObjectId { get; init; } = string.Empty;
    public string TransId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public long RecordId { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Data { get; init; }
        = new(StringComparer.OrdinalIgnoreCase);
}
