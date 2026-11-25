using System.Data;
using System.Reflection;
using ARMCommon.Interface;
using ARMCommon.Model;

namespace AxData.IntegrationTests.Sql;

// DispatchProxy generates a runtime subclass, so this test-only proxy cannot be sealed.
internal class RecordingDbHelper : DispatchProxy
{
    private readonly Dictionary<string, RecordedSnapshot> _rows = new(StringComparer.OrdinalIgnoreCase);

    public string Provider { get; private set; } = string.Empty;

    public string? LastSql { get; private set; }

    public IReadOnlyList<string>? LastParameterNames { get; private set; }

    public IReadOnlyList<DbType>? LastParameterTypes { get; private set; }

    public IReadOnlyList<object>? LastParameterValues { get; private set; }

    public static RecordingDbHelper Create(string provider, out IDbHelper helper)
    {
        helper = DispatchProxy.Create<IDbHelper, RecordingDbHelper>();
        var recorder = (RecordingDbHelper)(object)helper;
        recorder.Provider = provider;
        return recorder;
    }

    public string? GetPayload(string tenantId, string objectId) =>
        _rows.TryGetValue(Key(tenantId, objectId), out var row) ? row.Payload : null;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        if (targetMethod.Name == nameof(IDbHelper.GetDBType))
        {
            return Provider;
        }

        if (targetMethod.Name == nameof(IDbHelper.ExecuteNonQueryAsync)
            && args is [string sql, string[] names, DbType[] types, object[] values])
        {
            LastSql = sql;
            LastParameterNames = names;
            LastParameterTypes = types;
            LastParameterValues = values;

            var parameters = names
                .Select((name, index) => (name, values[index]))
                .ToDictionary(item => item.name, item => item.Item2, StringComparer.Ordinal);
            var tenantId = (string)parameters["TenantId"];
            var objectId = (string)parameters["ObjectId"];
            var key = Key(tenantId, objectId);
            _rows[key] = new RecordedSnapshot(
                tenantId,
                objectId,
                (string)parameters["LifecycleStatus"],
                (string)parameters["DataObjectJson"]);

            return Task.FromResult(new NonQueryResult
            {
                error = string.Empty,
                message = string.Empty,
                count = 1
            });
        }

        if (targetMethod.Name == nameof(IDbHelper.ExecuteSQLAsync)
            && args is [string querySql, string[] queryNames, DbType[] queryTypes, object[] queryValues])
        {
            LastSql = querySql;
            LastParameterNames = queryNames;
            LastParameterTypes = queryTypes;
            LastParameterValues = queryValues;
            var parameters = queryNames
                .Select((name, index) => (name, queryValues[index]))
                .ToDictionary(item => item.name, item => item.Item2, StringComparer.Ordinal);
            var table = new DataTable();
            table.Columns.Add("TenantId", typeof(string));
            table.Columns.Add("ObjectId", typeof(string));
            table.Columns.Add("DataObjectJson", typeof(string));
            table.Columns.Add("LifecycleStatus", typeof(string));
            if (_rows.TryGetValue(Key((string)parameters["TenantId"], (string)parameters["ObjectId"]), out var stored))
            {
                table.Rows.Add(
                    stored.TenantId,
                    stored.ObjectId,
                    stored.Payload,
                    stored.LifecycleStatus);
            }

            return Task.FromResult(new SQLResult
            {
                error = string.Empty,
                success = true,
                count = table.Rows.Count,
                data = table
            });
        }

        throw new NotSupportedException($"The test DB helper does not support '{targetMethod.Name}'.");
    }

    private static string Key(string tenantId, string objectId) => $"{tenantId}\u001f{objectId}";

    private sealed record RecordedSnapshot(
        string TenantId,
        string ObjectId,
        string LifecycleStatus,
        string Payload);
}
