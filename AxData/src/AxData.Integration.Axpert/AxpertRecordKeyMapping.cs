using System.Globalization;
using AxData.Application.Ports.Axpert;

namespace AxData.Integration.Axpert;

internal static class AxpertRecordKeyMapping
{
    public static string Field(AxRecordKey key) =>
        !string.IsNullOrWhiteSpace(key.KeyField)
            ? key.KeyField
            : key.RecordId is > 0 ? "recordid" : string.Empty;

    public static string? Value(AxRecordKey key) =>
        !string.IsNullOrWhiteSpace(key.KeyValue)
            ? key.KeyValue
            : key.RecordId?.ToString(CultureInfo.InvariantCulture);
}
