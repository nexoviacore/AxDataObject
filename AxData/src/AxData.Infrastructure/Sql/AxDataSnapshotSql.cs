namespace AxData.Infrastructure.Sql;

internal static class AxDataSnapshotSql
{
    internal const string Oracle = """
        MERGE INTO AxDataObjectInstance target
        USING (
            SELECT
                :TenantId TenantId,
                :ObjectId ObjectId,
                :TransId TransId,
                :KeyField KeyField,
                :KeyValue KeyValue,
                :RecordId RecordId,
                :DataObjectJson DataObjectJson,
                :ObjectVersion ObjectVersion,
                :StructDefVersion StructDefVersion,
                :OwnerUserName OwnerUserName,
                :InitiatorUserName InitiatorUserName,
                :PersistDateTimeUtc PersistDateTimeUtc,
                :LastAccessedDateTimeUtc LastAccessedDateTimeUtc,
                :LifecycleStatus LifecycleStatus,
                :ApprovalStatus ApprovalStatus,
                :PendingWith PendingWith,
                :PendingFromDateTimeUtc PendingFromDateTimeUtc,
                :CurrentTaskId CurrentTaskId,
                :AxOpen AxOpen
            FROM dual
        ) incoming
        ON (target.TenantId = incoming.TenantId AND target.ObjectId = incoming.ObjectId)
        WHEN MATCHED THEN UPDATE SET
            target.TransId = incoming.TransId,
            target.KeyField = incoming.KeyField,
            target.KeyValue = incoming.KeyValue,
            target.RecordId = incoming.RecordId,
            target.DataObjectJson = incoming.DataObjectJson,
            target.ObjectVersion = incoming.ObjectVersion,
            target.StructDefVersion = incoming.StructDefVersion,
            target.OwnerUserName = incoming.OwnerUserName,
            target.InitiatorUserName = incoming.InitiatorUserName,
            target.PersistDateTimeUtc = incoming.PersistDateTimeUtc,
            target.LastAccessedDateTimeUtc = incoming.LastAccessedDateTimeUtc,
            target.LifecycleStatus = incoming.LifecycleStatus,
            target.ApprovalStatus = incoming.ApprovalStatus,
            target.PendingWith = incoming.PendingWith,
            target.PendingFromDateTimeUtc = incoming.PendingFromDateTimeUtc,
            target.CurrentTaskId = incoming.CurrentTaskId,
            target.AxOpen = incoming.AxOpen
        WHERE incoming.ObjectVersion > target.ObjectVersion
        WHEN NOT MATCHED THEN INSERT (
            TenantId, ObjectId, TransId, KeyField, KeyValue, RecordId, DataObjectJson,
            ObjectVersion, StructDefVersion, OwnerUserName, InitiatorUserName,
            PersistDateTimeUtc, LastAccessedDateTimeUtc, LifecycleStatus, ApprovalStatus,
            PendingWith, PendingFromDateTimeUtc, CurrentTaskId, AxOpen
        ) VALUES (
            incoming.TenantId, incoming.ObjectId, incoming.TransId, incoming.KeyField,
            incoming.KeyValue, incoming.RecordId, incoming.DataObjectJson,
            incoming.ObjectVersion, incoming.StructDefVersion, incoming.OwnerUserName,
            incoming.InitiatorUserName, incoming.PersistDateTimeUtc,
            incoming.LastAccessedDateTimeUtc, incoming.LifecycleStatus,
            incoming.ApprovalStatus, incoming.PendingWith, incoming.PendingFromDateTimeUtc,
            incoming.CurrentTaskId, incoming.AxOpen
        )
        """;

    internal const string Postgres = """
        INSERT INTO AxDataObjectInstance (
            TenantId, ObjectId, TransId, KeyField, KeyValue, RecordId, DataObjectJson,
            ObjectVersion, StructDefVersion, OwnerUserName, InitiatorUserName,
            PersistDateTimeUtc, LastAccessedDateTimeUtc, LifecycleStatus, ApprovalStatus,
            PendingWith, PendingFromDateTimeUtc, CurrentTaskId, AxOpen
        ) VALUES (
            @TenantId, @ObjectId, @TransId, @KeyField, @KeyValue, @RecordId, @DataObjectJson,
            @ObjectVersion, @StructDefVersion, @OwnerUserName, @InitiatorUserName,
            CAST(@PersistDateTimeUtc AS timestamptz), CAST(@LastAccessedDateTimeUtc AS timestamptz),
            @LifecycleStatus, @ApprovalStatus, @PendingWith,
            CAST(@PendingFromDateTimeUtc AS timestamptz), @CurrentTaskId, @AxOpen
        )
        ON CONFLICT (TenantId, ObjectId) DO UPDATE SET
            TransId = EXCLUDED.TransId,
            KeyField = EXCLUDED.KeyField,
            KeyValue = EXCLUDED.KeyValue,
            RecordId = EXCLUDED.RecordId,
            DataObjectJson = EXCLUDED.DataObjectJson,
            ObjectVersion = EXCLUDED.ObjectVersion,
            StructDefVersion = EXCLUDED.StructDefVersion,
            OwnerUserName = EXCLUDED.OwnerUserName,
            InitiatorUserName = EXCLUDED.InitiatorUserName,
            PersistDateTimeUtc = EXCLUDED.PersistDateTimeUtc,
            LastAccessedDateTimeUtc = EXCLUDED.LastAccessedDateTimeUtc,
            LifecycleStatus = EXCLUDED.LifecycleStatus,
            ApprovalStatus = EXCLUDED.ApprovalStatus,
            PendingWith = EXCLUDED.PendingWith,
            PendingFromDateTimeUtc = EXCLUDED.PendingFromDateTimeUtc,
            CurrentTaskId = EXCLUDED.CurrentTaskId,
            AxOpen = EXCLUDED.AxOpen
        WHERE EXCLUDED.ObjectVersion > AxDataObjectInstance.ObjectVersion
        """;

    internal const string SqlServer = """
        MERGE AxDataObjectInstance WITH (HOLDLOCK) AS target
        USING (VALUES (
            @TenantId, @ObjectId, @TransId, @KeyField, @KeyValue, @RecordId, @DataObjectJson,
            @ObjectVersion, @StructDefVersion, @OwnerUserName, @InitiatorUserName,
            @PersistDateTimeUtc, @LastAccessedDateTimeUtc, @LifecycleStatus, @ApprovalStatus,
            @PendingWith, @PendingFromDateTimeUtc, @CurrentTaskId, @AxOpen
        )) AS incoming (
            TenantId, ObjectId, TransId, KeyField, KeyValue, RecordId, DataObjectJson,
            ObjectVersion, StructDefVersion, OwnerUserName, InitiatorUserName,
            PersistDateTimeUtc, LastAccessedDateTimeUtc, LifecycleStatus, ApprovalStatus,
            PendingWith, PendingFromDateTimeUtc, CurrentTaskId, AxOpen
        )
        ON target.TenantId = incoming.TenantId AND target.ObjectId = incoming.ObjectId
        WHEN MATCHED AND incoming.ObjectVersion > target.ObjectVersion THEN UPDATE SET
            TransId = incoming.TransId,
            KeyField = incoming.KeyField,
            KeyValue = incoming.KeyValue,
            RecordId = incoming.RecordId,
            DataObjectJson = incoming.DataObjectJson,
            ObjectVersion = incoming.ObjectVersion,
            StructDefVersion = incoming.StructDefVersion,
            OwnerUserName = incoming.OwnerUserName,
            InitiatorUserName = incoming.InitiatorUserName,
            PersistDateTimeUtc = incoming.PersistDateTimeUtc,
            LastAccessedDateTimeUtc = incoming.LastAccessedDateTimeUtc,
            LifecycleStatus = incoming.LifecycleStatus,
            ApprovalStatus = incoming.ApprovalStatus,
            PendingWith = incoming.PendingWith,
            PendingFromDateTimeUtc = incoming.PendingFromDateTimeUtc,
            CurrentTaskId = incoming.CurrentTaskId,
            AxOpen = incoming.AxOpen
        WHEN NOT MATCHED THEN INSERT (
            TenantId, ObjectId, TransId, KeyField, KeyValue, RecordId, DataObjectJson,
            ObjectVersion, StructDefVersion, OwnerUserName, InitiatorUserName,
            PersistDateTimeUtc, LastAccessedDateTimeUtc, LifecycleStatus, ApprovalStatus,
            PendingWith, PendingFromDateTimeUtc, CurrentTaskId, AxOpen
        ) VALUES (
            incoming.TenantId, incoming.ObjectId, incoming.TransId, incoming.KeyField,
            incoming.KeyValue, incoming.RecordId, incoming.DataObjectJson,
            incoming.ObjectVersion, incoming.StructDefVersion, incoming.OwnerUserName,
            incoming.InitiatorUserName, incoming.PersistDateTimeUtc,
            incoming.LastAccessedDateTimeUtc, incoming.LifecycleStatus,
            incoming.ApprovalStatus, incoming.PendingWith, incoming.PendingFromDateTimeUtc,
            incoming.CurrentTaskId, incoming.AxOpen
        );
        """;

    internal static string ForProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return provider.Trim().ToLowerInvariant() switch
        {
            "oracle" => Oracle,
            "postgre" or "postgres" or "postgresql" => Postgres,
            "ms sql" or "mssql" or "sqlserver" or "sql server" => SqlServer,
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported for snapshot persistence.")
        };
    }

    internal static string ForReadProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        var marker = provider.Trim().Equals("oracle", StringComparison.OrdinalIgnoreCase) ? ":" : "@";
        return $"""
            SELECT TenantId, ObjectId, DataObjectJson, ObjectVersion, StructDefVersion, LifecycleStatus
            FROM AxDataObjectInstance
            WHERE TenantId = {marker}TenantId AND ObjectId = {marker}ObjectId
            """;
    }
}
