using System.Data;
using System.Text;
using System.Text.Json;
using ARMCommon.Interface;
using AxData.Application.Ports;
using AxData.Domain.Lifecycle;
using AxData.Infrastructure.Serialization;

namespace AxData.Infrastructure.Sql;

public sealed class AxDataSnapshotStore : IAxDataSnapshotRepository
{
    private static readonly string[] ParameterNames =
    [
        "TenantId", "ObjectId", "TransId", "KeyField", "KeyValue", "RecordId",
        "DataObjectJson", "ObjectVersion", "StructDefVersion", "OwnerUserName",
        "InitiatorUserName", "PersistDateTimeUtc", "LastAccessedDateTimeUtc",
        "LifecycleStatus", "ApprovalStatus", "PendingWith", "PendingFromDateTimeUtc",
        "CurrentTaskId", "AxOpen"
    ];

    private static readonly DbType[] ParameterTypes =
    [
        DbType.String, DbType.String, DbType.String, DbType.String, DbType.String,
        DbType.Int64, DbType.Object, DbType.Int64, DbType.String, DbType.String,
        DbType.String, DbType.DateTime, DbType.DateTime, DbType.String, DbType.String,
        DbType.Object, DbType.DateTime, DbType.String, DbType.Int32
    ];

    private readonly IDbHelper _dbHelper;
    private readonly AxDataSnapshotSerializer _serializer;

    public AxDataSnapshotStore(IDbHelper dbHelper, AxDataSnapshotSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(dbHelper);
        ArgumentNullException.ThrowIfNull(serializer);
        _dbHelper = dbHelper;
        _serializer = serializer;
    }

    public async Task<AxDataSnapshotUpsertStatus> UpsertAsync(
        AxDataSnapshotWrite write,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentException.ThrowIfNullOrWhiteSpace(write.EventId);
        ArgumentNullException.ThrowIfNull(write.State);
        cancellationToken.ThrowIfCancellationRequested();

        var state = write.State;
        var workflow = state.Workflow;
        var isPending = workflow?.Status == PegWorkflowStatus.Pending;
        var pendingWith = JsonSerializer.Serialize(
            isPending ? workflow!.Approvers : Array.Empty<string>());
        var payload = Encoding.UTF8.GetString(_serializer.Serialize(state));
        var provider = _dbHelper.GetDBType();
        var sql = AxDataSnapshotSql.ForProvider(provider);
        var isPostgres = IsPostgres(provider);
        var parameterTypes = isPostgres ? PostgresParameterTypes() : ParameterTypes;

        object[] parameterValues =
        [
            state.TenantId,
            state.ObjectId,
            state.TransId,
            DbValue(state.KeyField),
            DbValue(state.KeyValue),
            DbValue(state.RecordId),
            payload,
            state.Version,
            state.StructDefVersion,
            state.OwnerUserName,
            state.InitiatorUserName,
            DatabaseTimestamp(write.PersistedAtUtc, isPostgres),
            DatabaseTimestamp(state.LastAccessedAtUtc, isPostgres),
            state.Status.ToString(),
            (workflow?.Status ?? PegWorkflowStatus.None).ToString(),
            pendingWith,
            isPending ? DatabaseTimestamp(workflow!.PendingFromUtc, isPostgres) : DBNull.Value,
            DbValue(workflow?.CurrentTaskId),
            state.AxOpen ? 1 : 0
        ];

        var result = await _dbHelper.ExecuteNonQueryAsync(
                sql,
                ParameterNames,
                parameterTypes,
                parameterValues)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(result.error))
        {
            throw new InvalidOperationException("The AxData snapshot upsert failed.");
        }

        return result.count > 0
            ? AxDataSnapshotUpsertStatus.Stored
            : AxDataSnapshotUpsertStatus.Ignored;
    }

    public async Task<SnapshotPersistStatus> UpsertAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        try
        {
            var status = await UpsertAsync(
                new AxDataSnapshotWrite(envelope.EventId, envelope.Snapshot, envelope.PersistedAtUtc),
                cancellationToken).ConfigureAwait(false);
            return status == AxDataSnapshotUpsertStatus.Stored
                ? SnapshotPersistStatus.Stored
                : SnapshotPersistStatus.Ignored;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return SnapshotPersistStatus.Unavailable;
        }
    }

    public async Task<SnapshotReadResult> LoadLatestAsync(
        string tenantId,
        string objectId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await _dbHelper.ExecuteSQLAsync(
                    AxDataSnapshotSql.ForReadProvider(_dbHelper.GetDBType()),
                    ["TenantId", "ObjectId"],
                    [DbType.String, DbType.String],
                    [tenantId, objectId])
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.error))
            {
                return new SnapshotReadResult(SnapshotReadStatus.Unavailable);
            }

            if (result.data.Rows.Count == 0)
            {
                return new SnapshotReadResult(SnapshotReadStatus.NotFound);
            }

            if (result.data.Rows.Count != 1)
            {
                return new SnapshotReadResult(SnapshotReadStatus.Invalid);
            }

            var row = result.data.Rows[0];
            var payload = Convert.ToString(row["DataObjectJson"], System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new SnapshotReadResult(SnapshotReadStatus.Invalid);
            }

            var state = _serializer.Deserialize(Encoding.UTF8.GetBytes(payload));
            var metadataMatches = string.Equals(Convert.ToString(row["TenantId"]), state.TenantId, StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(Convert.ToString(row["ObjectId"]), state.ObjectId, StringComparison.OrdinalIgnoreCase) &&
                                  Convert.ToInt64(row["ObjectVersion"], System.Globalization.CultureInfo.InvariantCulture) == state.Version &&
                                  string.Equals(Convert.ToString(row["StructDefVersion"]), state.StructDefVersion, StringComparison.Ordinal) &&
                                  string.Equals(Convert.ToString(row["LifecycleStatus"]), state.Status.ToString(), StringComparison.Ordinal);
            return metadataMatches
                ? SnapshotReadResult.Found(state)
                : new SnapshotReadResult(SnapshotReadStatus.Invalid);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AxDataSnapshotException)
        {
            return new SnapshotReadResult(SnapshotReadStatus.Invalid);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidCastException or OverflowException)
        {
            return new SnapshotReadResult(SnapshotReadStatus.Invalid);
        }
        catch
        {
            return new SnapshotReadResult(SnapshotReadStatus.Unavailable);
        }
    }

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static bool IsPostgres(string provider) =>
        provider.Trim().Equals("postgre", StringComparison.OrdinalIgnoreCase) ||
        provider.Trim().Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
        provider.Trim().Equals("postgresql", StringComparison.OrdinalIgnoreCase);

    private static DbType[] PostgresParameterTypes()
    {
        var types = (DbType[])ParameterTypes.Clone();
        types[11] = DbType.String;
        types[12] = DbType.String;
        types[16] = DbType.String;
        return types;
    }

    private static object DatabaseTimestamp(DateTimeOffset value, bool isPostgres) =>
        isPostgres
            ? value.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            : value.UtcDateTime;

}
