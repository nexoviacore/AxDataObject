using System.Data;
using ARMCommon.Interface;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Authorization;
using AxData.Integration.Axpert.AxGet;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using AxData.Domain.Model;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.ContractTests;

public sealed class RealDataReviewSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Review_cache_sink_never_opens_a_real_Redis_connection()
    {
        var cache = NoOpRedisCache.Create();

        Assert.True(cache.Redis.IsConnected);
        Assert.Null(await cache.ObjectGetAsync("review-key"));
        Assert.False(await cache.ObjectSetAsync("review-key", new object()));
    }

    [Fact]
    public async Task Real_create_view_edit_permissions_are_characterized_when_review_environment_is_configured()
    {
        if (!TryEnvironment(out var environment) || environment.RecordId is null)
        {
            return;
        }

        var options = CreateOptions(environment);
        var contextFactory = new AxpertRuntimeContextFactory(new SilentTraceService(), options);
        var structDefSource = new AxpertStructDefSource(contextFactory);
        var permissionSource = new AxpertPermissionSource(contextFactory);
        var authorization = new AxpertAuthorizationProvider(permissionSource, structDefSource);
        var user = new AxUserContext(
            environment.TenantId,
            environment.UserName,
            new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase));

        var createPermission = await authorization.GetCreatePermissionAsync(
            environment.TenantId,
            environment.TransId,
            user,
            CancellationToken.None);
        var queryPermission = await authorization.GetQueryPermissionAsync(
            environment.TenantId,
            environment.TransId,
            new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
            user,
            CancellationToken.None);
        var setPermission = await authorization.GetSetPermissionAsync(
            environment.TenantId,
            environment.TransId,
            new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
            user,
            CancellationToken.None);
        var rawView = await permissionSource.LoadAsync(
            new AxpertPermissionRequest(
                environment.TenantId,
                environment.TransId,
                new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
                user.UserName,
                user.Roles,
                AxpertPermissionKind.View),
            CancellationToken.None);
        var rawEdit = await permissionSource.LoadAsync(
            new AxpertPermissionRequest(
                environment.TenantId,
                environment.TransId,
                new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
                user.UserName,
                user.Roles,
                AxpertPermissionKind.Edit),
            CancellationToken.None);
        var crossTenantPermission = await authorization.GetQueryPermissionAsync(
            environment.TenantId,
            environment.TransId,
            new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
            user with { TenantId = "cross-tenant-review" },
            CancellationToken.None);
        var unrelatedPermission = await authorization.GetQueryPermissionAsync(
            environment.TenantId,
            environment.TransId,
            new AxRecordKey(environment.KeyField, environment.KeyValue, environment.RecordId),
            new AxUserContext(
                environment.TenantId,
                "__axdata_unrelated_review__",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
            CancellationToken.None);

        output.WriteLine(
            "Authorization create={0}, query={1}, set={2}, crossTenantDenied={3}, unrelatedDenied={4}, rawViewSucceeded={5}, rawViewRows={6}, rawViewError={7}",
            createPermission.IsAllowed,
            queryPermission.IsAllowed,
            setPermission.IsAllowed,
            !crossTenantPermission.IsAllowed,
            !unrelatedPermission.IsAllowed,
            rawView.Succeeded,
            rawView.Rows.Count,
            rawView.Error ?? "none");
        output.WriteLine(
            "Raw view policy={0}; raw edit policy={1}",
            string.Join("|", rawView.Rows.Select(row => $"{row.PermissionType}:{row.FullControl}:{row.Access}")),
            string.Join("|", rawEdit.Rows.Select(row => $"{row.PermissionType}:{row.FullControl}:{row.Access}")));
        Assert.True(createPermission.IsAllowed);
        Assert.Equal(rawView.Succeeded && rawView.Rows.Any(AllowsRecordAccess), queryPermission.IsAllowed);
        Assert.Equal(rawEdit.Succeeded && rawEdit.Rows.Any(AllowsRecordAccess), setPermission.IsAllowed);
        Assert.False(crossTenantPermission.IsAllowed);
        Assert.False(unrelatedPermission.IsAllowed);
    }

    [Fact]
    public async Task Current_structdef_and_field_runtime_load_when_review_environment_is_configured()
    {
        if (!TryEnvironment(out var environment))
        {
            return;
        }

        var options = CreateOptions(environment);
        var contextFactory = new AxpertRuntimeContextFactory(new SilentTraceService(), options);
        var source = new AxpertStructDefSource(contextFactory);

        var context = await contextFactory.CreateAsync(environment.TenantId, CancellationToken.None);
        Assert.True(context.Succeeded, context.Error);
        var rows = await context.Value.Common._dbHelper.ExecuteSQLAsync(
            $"SELECT props, blobno, updatedon, runtimetstruct, runtimemod FROM \"{environment.TenantId}\".tstructs WHERE LOWER(name) = @name ORDER BY blobno",
            ["@name"],
            [DbType.String],
            [environment.TransId.ToLowerInvariant()]);
        Assert.True(string.IsNullOrWhiteSpace(rows.error), rows.error);
        Assert.NotEmpty(rows.data.Rows.Cast<DataRow>());
        Assert.NotNull(AxpertStructDefSource.ComputeContentHash(rows));
        var legacyDefinition = await LegacyStructDef.LoadStructure(
            context.Value.Common,
            environment.TransId,
            environment.Roles.ToList(),
            context.Value.TraceService,
            refreshCache: true);
        Assert.NotNull(legacyDefinition);

        var snapshot = await source.LoadCurrentAsync(
            environment.TenantId,
            environment.TransId,
            CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.Matches("^[0-9a-f]{64}$", snapshot.ContentHash);
        Assert.NotEmpty(snapshot.DataContainers);
        Assert.NotEmpty(snapshot.DataContainers.SelectMany(container => container.Fields));
        output.WriteLine(
            "StructDef tenant={0}, transId={1}, hash={2}, updatedOn={3}, legacyVersion={4}, runtimeStruct={5}, runtimeMode={6}, containers={7}, fields={8}, dependencies={9}, fillGrids={10}",
            snapshot.TenantId,
            snapshot.TransId,
            snapshot.ContentHash,
            snapshot.UpdatedOn ?? "<null>",
            snapshot.LegacyVersionNo ?? "<null>",
            snapshot.RunTimeStruct,
            snapshot.RunTimeMode,
            snapshot.DataContainers.Count,
            snapshot.DataContainers.Sum(container => container.Fields.Count),
            snapshot.Dependencies.Count,
            snapshot.FillGrids.Count);

        var runtimeFactory = new AxpertFieldRuntimeFactory(contextFactory, source);
        var runtime = await runtimeFactory.CreateAsync(
            new AxpertFieldRuntimeRequest(
                environment.TenantId,
                environment.TransId,
                snapshot.ContentHash,
                new AxUserContext(
                    environment.TenantId,
                    environment.UserName,
                    new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase))),
            CancellationToken.None);

        Assert.True(runtime.Succeeded, runtime.Error);

        if (environment.RecordId is not null)
        {
            var recordKey = !string.IsNullOrWhiteSpace(environment.KeyField) &&
                            !string.IsNullOrWhiteSpace(environment.KeyValue)
                ? new AxRecordKey(environment.KeyField, environment.KeyValue, null)
                : new AxRecordKey(null, null, environment.RecordId);
            var dataSource = new AxpertDataSource(contextFactory, source);
            var loaded = await dataSource.LoadAsync(
                new AxpertDataSourceRequest(
                    environment.TenantId,
                    environment.TransId,
                    snapshot.ContentHash,
                    recordKey,
                    environment.UserName,
                    new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase)),
                CancellationToken.None);

            Assert.True(loaded.Succeeded, loaded.Error);
            Assert.Equal(environment.RecordId, loaded.RecordId);
            Assert.NotNull(loaded.DataContainers);
            Assert.NotEmpty(loaded.DataContainers);
            var definitionsByContainer = snapshot.DataContainers.ToDictionary(
                container => container.Name,
                StringComparer.OrdinalIgnoreCase);
            var unknownContainers = loaded.DataContainers
                .Where(container => !definitionsByContainer.ContainsKey(container.Name))
                .Select(container => container.Name)
                .ToArray();
            Assert.True(
                unknownContainers.Length == 0,
                $"Unknown AxGet containers: {string.Join(',', unknownContainers)}");
            var unknownFields = loaded.DataContainers
                .Where(container => definitionsByContainer.ContainsKey(container.Name))
                .SelectMany(container => container.Rows.SelectMany(row => row.Fields.Keys.Select(field =>
                    (container.Name, Field: field))))
                .Where(item => definitionsByContainer[item.Name].Fields.All(field =>
                    !string.Equals(field.Name, item.Field, StringComparison.OrdinalIgnoreCase)))
                .Select(item => $"{item.Name}.{item.Field}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.True(
                unknownFields.Length == 0,
                $"Unknown AxGet fields: {string.Join(',', unknownFields)}");

            var definition = new StructDefHandle(
                snapshot.TenantId,
                snapshot.TransId,
                snapshot.ContentHash,
                snapshot.DataContainers,
                snapshot.Dependencies,
                snapshot.FillGrids);
            var user = new AxUserContext(
                environment.TenantId,
                environment.UserName,
                new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase));
            var loader = new AxpertDataLoader(dataSource, new CryptographicRowIdGenerator());
            var stateResult = await loader.LoadAsync(
                definition,
                recordKey,
                user,
                CancellationToken.None);
            Assert.True(stateResult.Succeeded, stateResult.Error);
            Assert.NotNull(stateResult.State);

            var firstRow = Assert.Single(stateResult.State.DataContainers["dc1"].Rows);
            var fieldEngine = new AxpertFieldEngine(runtimeFactory);
            var evaluation = await fieldEngine.SetValidateAndRefreshAsync(
                stateResult.State,
                new FieldAddress("dc1", firstRow.RowId, "qty"),
                "6",
                user,
                CancellationToken.None);

            Assert.True(evaluation.Succeeded, evaluation.Errors.FirstOrDefault()?.Message);
            Assert.Contains(
                evaluation.Changes,
                change => change.Source == FieldChangeSource.Dependency);
            output.WriteLine(
                "AxGet recordId={0}, containers={1}; Validate/Parser dependency evaluation=success",
                loaded.RecordId,
                loaded.DataContainers.Count);

        }
    }

    [Fact]
    public async Task Real_FillGrid_metadata_and_tenant_schema_are_characterized_when_configured()
    {
        if (!TryEnvironment(out var environment) ||
            !int.TryParse(Get("AXDATA_REVIEW_FILLGRID_ADDROWS"), out var expectedAddRows))
        {
            return;
        }

        Assert.InRange(expectedAddRows, 1, 3);
        var contextFactory = new AxpertRuntimeContextFactory(
            new SilentTraceService(),
            CreateOptions(environment));
        var context = await contextFactory.CreateAsync(environment.TenantId, CancellationToken.None);
        Assert.True(context.Succeeded, context.Error);
        var schemaResult = await context.Value.Common._dbHelper.ExecuteScalarAsync("SELECT current_schema()");
        Assert.True(string.IsNullOrWhiteSpace(schemaResult.error), schemaResult.error);
        Assert.Equal(
            environment.TenantId,
            Convert.ToString(schemaResult.result, System.Globalization.CultureInfo.InvariantCulture),
            ignoreCase: true);

        var definition = await LegacyStructDef.LoadStructure(
            context.Value.Common,
            environment.TransId,
            environment.Roles.ToList(),
            context.Value.TraceService,
            refreshCache: true);
        Assert.NotNull(definition);
        Assert.NotNull(definition.Fgs);
        var sqlResult = await AxpertFillGridSqlSource.LoadAsync(
            context.Value,
            environment.TenantId,
            environment.TransId,
            CancellationToken.None);
        Assert.True(sqlResult.Succeeded, sqlResult.Error);
        Assert.NotNull(sqlResult.Value);
        var configuredName = Get("AXDATA_REVIEW_FILLGRID_NAME");
        var matches = definition.Fgs
            .Where(item => item.AddRows == expectedAddRows)
            .Where(item => string.IsNullOrWhiteSpace(configuredName) ||
                           string.Equals(item.Fname, configuredName, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(item.Name, configuredName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(matches);

        foreach (var item in matches)
        {
            var name = string.IsNullOrWhiteSpace(item.Fname) ? item.Name : item.Fname;
            Assert.True(sqlResult.Value.TryGetValue(name, out var sqlText));
            Assert.False(string.IsNullOrWhiteSpace(sqlText));
            var query = AxpertFillGridRuntime.FromLegacy(item) with { SqlText = sqlText };
            var rawParameterCount = item.ParamNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Length;
            var parameterOccurrences = Regex.Matches(
                    query.SqlText,
                    "(?<![A-Za-z0-9_:@])[:@](?<name>[A-Za-z_][A-Za-z0-9_]*)",
                    RegexOptions.CultureInvariant)
                .Select(match => match.Groups["name"].Value)
                .ToArray();
            var duplicateParameterCount = rawParameterCount - query.ParameterNames.Count +
                parameterOccurrences.Length -
                parameterOccurrences.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var mode = item.AddRows == 2 ? FillGridMode.Replace : FillGridMode.Append;
            Assert.Equal(expectedAddRows, query.AddRows);
            output.WriteLine(
                "FillGrid tenant={0}, transId={1}, name={2}, addRows={3}, mode={4}, autoShow={5}, firmBind={6}, multiSelect={7}, parameters={8}, duplicateParameters={9}, mappings={10}, sqlHash={11}, schemaVerified=true",
                environment.TenantId,
                environment.TransId,
                string.IsNullOrWhiteSpace(query.Name) ? "<unnamed>" : query.Name,
                query.AddRows,
                mode,
                query.AutoShow,
                query.FirmBind,
                query.MultiSelect,
                query.ParameterNames.Count,
                duplicateParameterCount,
                query.TargetToSourceFields.Count,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(query.SqlText))).ToLowerInvariant());
        }
    }

    [Fact]
    public async Task Real_FillGrid_queries_execute_for_empty_targets_when_configured()
    {
        if (!TryEnvironment(out var environment) ||
            !string.Equals(Get("AXDATA_REVIEW_EXECUTE_FILLGRID"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var contextFactory = new AxpertRuntimeContextFactory(
            new SilentTraceService(),
            CreateOptions(environment));
        var source = new AxpertStructDefSource(contextFactory);
        var snapshot = await source.LoadCurrentAsync(
            environment.TenantId,
            environment.TransId,
            CancellationToken.None);
        Assert.NotNull(snapshot);

        var state = new AxDataState(
            "fillgrid-review",
            environment.TenantId,
            environment.TransId,
            environment.UserName,
            environment.UserName,
            snapshot.ContentHash,
            DateTimeOffset.UnixEpoch);
        foreach (var container in snapshot.DataContainers)
        {
            state.DataContainers.Add(
                container.Name,
                new AxDataContainer(container.Name, container.IsGrid));
        }

        var engine = new AxpertFillGridEngine(new AxpertFillGridRuntimeFactory(
            contextFactory,
            source,
            new AxpertFillGridQueryOptions()));
        var result = await engine.ExecuteApplicableAsync(
            state,
            [],
            new AxUserContext(
                environment.TenantId,
                environment.UserName,
                new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase)),
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(3, result.Operations.Count);
        Assert.All(result.Operations, operation =>
        {
            Assert.True(operation.Succeeded, operation.Error);
            Assert.InRange(operation.Rows.Count, 1, 10);
        });
        output.WriteLine(
            "FillGrid execution tenant={0}, transId={1}, operations={2}, totalRows={3}, modes={4}",
            environment.TenantId,
            environment.TransId,
            result.Operations.Count,
            result.Operations.Sum(operation => operation.Rows.Count),
            string.Join('|', result.Operations.Select(operation => operation.Mode)));
    }

    [Fact]
    public async Task Real_FillGrid_record_and_editable_parent_metadata_are_characterized_when_configured()
    {
        if (!TryEnvironment(out var environment) || environment.RecordId is null ||
            !string.Equals(Get("AXDATA_REVIEW_EXECUTE_FILLGRID"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var contextFactory = new AxpertRuntimeContextFactory(
            new SilentTraceService(),
            CreateOptions(environment));
        var source = new AxpertStructDefSource(contextFactory);
        var snapshot = await source.LoadCurrentAsync(
            environment.TenantId,
            environment.TransId,
            CancellationToken.None);
        Assert.NotNull(snapshot);

        var dataSource = new AxpertDataSource(contextFactory, source);
        var loaded = await dataSource.LoadAsync(
            new AxpertDataSourceRequest(
                environment.TenantId,
                environment.TransId,
                snapshot.ContentHash,
                new AxRecordKey(null, null, environment.RecordId),
                environment.UserName,
                new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase)),
            CancellationToken.None);
        Assert.True(loaded.Succeeded, loaded.Error);
        Assert.Equal(environment.RecordId, loaded.RecordId);
        Assert.NotNull(loaded.DataContainers);
        var user = new AxUserContext(
            environment.TenantId,
            environment.UserName,
            new HashSet<string>(environment.Roles, StringComparer.OrdinalIgnoreCase));
        var stateResult = await new AxpertDataLoader(dataSource, new CryptographicRowIdGenerator()).LoadAsync(
            new StructDefHandle(
                snapshot.TenantId,
                snapshot.TransId,
                snapshot.ContentHash,
                snapshot.DataContainers,
                snapshot.Dependencies,
                snapshot.FillGrids),
            new AxRecordKey(null, null, environment.RecordId),
            user,
            CancellationToken.None);
        Assert.True(stateResult.Succeeded, stateResult.Error);
        Assert.NotNull(stateResult.State);
        var parentRow = Assert.Single(stateResult.State.DataContainers["dc1"].Rows);

        var context = await contextFactory.CreateAsync(environment.TenantId, CancellationToken.None);
        Assert.True(context.Succeeded, context.Error);
        var definition = await LegacyStructDef.LoadStructure(
            context.Value.Common,
            environment.TransId,
            environment.Roles.ToList(),
            context.Value.TraceService,
            refreshCache: true);
        Assert.NotNull(definition?.Flds);
        var candidates = definition.Flds!
            .Where(field => field.FrameNo == 1 && !field.Hidden && !field.Readonly)
            .Take(10)
            .Select(field => $"{field.FieldName}:{field.DataType}")
            .ToArray();
        Assert.NotEmpty(candidates);

        output.WriteLine(
            "FillGrid record tenant={0}, transId={1}, recordId={2}, parentRowId={3}, parentPersistedId={4}, containers={5}, rowCounts={6}, editableParentCandidates={7}",
            environment.TenantId,
            environment.TransId,
            loaded.RecordId,
            parentRow.RowId,
            parentRow.PersistedRecordId,
            loaded.DataContainers!.Count,
            string.Join('|', loaded.DataContainers.Select(container => $"{container.Name}:{container.Rows.Count}")),
            string.Join('|', candidates));
    }

    private static bool AllowsRecordAccess(AxpertPermissionRow row) =>
        string.Equals(row.FullControl, "T", StringComparison.OrdinalIgnoreCase) ||
        (string.IsNullOrWhiteSpace(row.FullControl) &&
         !string.Equals(row.Access, "NONE", StringComparison.OrdinalIgnoreCase));

    private static bool TryEnvironment(out ReviewEnvironment environment)
    {
        var tenantId = Get("AXDATA_REVIEW_TENANT");
        var transId = Get("AXDATA_REVIEW_TRANSID");
        var sessionId = Get("AXDATA_REVIEW_SESSION_ID");
        var userName = Get("AXDATA_REVIEW_USER");
        var roles = Get("AXDATA_REVIEW_ROLES");
        var databaseType = Get("AXDATA_REVIEW_DB_TYPE");
        var connectionString = Get("AXDATA_REVIEW_CONNECTION_STRING");
        if (new[] { tenantId, transId, sessionId, userName, roles, databaseType, connectionString }
            .Any(string.IsNullOrWhiteSpace))
        {
            environment = null!;
            return false;
        }

        environment = new ReviewEnvironment(
            tenantId!,
            transId!,
            sessionId!,
            userName!,
            roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            databaseType!,
            connectionString!,
            long.TryParse(Get("AXDATA_REVIEW_RECORD_ID"), out var recordId) ? recordId : null,
            Get("AXDATA_REVIEW_KEY_FIELD"),
            Get("AXDATA_REVIEW_KEY_VALUE"));
        return true;
    }

    private static AxpertReviewRuntimeOptions CreateOptions(ReviewEnvironment environment) => new()
    {
        TenantId = environment.TenantId,
        SessionId = environment.SessionId,
        UserName = environment.UserName,
        Roles = environment.Roles,
        DatabaseType = environment.DatabaseType,
        ConnectionString = environment.ConnectionString,
        AxpertSessionId = Environment.GetEnvironmentVariable("AXDATA_REVIEW_AXPERT_SESSION_ID")
    };

    private static string? Get(string name) => Environment.GetEnvironmentVariable(name);

    private sealed record ReviewEnvironment(
        string TenantId,
        string TransId,
        string SessionId,
        string UserName,
        IReadOnlyList<string> Roles,
        string DatabaseType,
        string ConnectionString,
        long? RecordId,
        string? KeyField,
        string? KeyValue);

    private sealed class SilentTraceService : IARMLogTraceService
    {
        public void EnableTracing(bool enabled, bool keepOldLogs = false) { }
        public bool IsTracingEnabled() => false;
        public void LogDbConnection(string connectionString) { }
        public void LogSqlExecution(string sql, string[]? paramNames = null, DbType[]? paramTypes = null, object[]? paramValues = null, Dictionary<string, string>? viewFilters = null, Dictionary<string, string>? globalParams = null, Dictionary<string, string>? sqlCommentsReplace = null) { }
        public void LogResultInfo(int rowCount, DateTime startTime, DateTime endTime) { }
        public void LogScalarResultInfo(string result, DateTime startTime, DateTime endTime) { }
        public void LogError(string errorMessage) { }
        public void LogMessage(string message) { }
        public void LogMessage(string title, string message) { }
        public void LogDataTable(DataTable table, DateTime? startTime = null, DateTime? endTime = null) { }
        public string GetTraceLog() => string.Empty;
        public Task<string> GetTraceFile(string fileName) => Task.FromResult(string.Empty);
        public void Clear() { }
    }
}
