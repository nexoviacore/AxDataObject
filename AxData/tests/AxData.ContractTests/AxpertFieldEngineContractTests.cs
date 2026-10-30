using System.Data;
using System.Reflection;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Integration.Axpert.FieldEngine;
using AxParserApp;
using AxStructDef;
using AxStructDef.Helpers;
using AxStructDef.Models;
using AxValidate;
using AxValidate.Helpers;

namespace AxData.ContractTests;

public sealed class AxpertFieldEngineContractTests
{
    private static readonly AxUserContext User = new(
        "sample-tenant",
        "owner",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "users" });

    [Fact]
    public async Task Ten_evaluations_share_one_runtime_and_complete_once()
    {
        var state = CreateState();
        var factory = new LegacyRuntimeFactory();
        var adapter = new AxpertFieldEngine(factory);
        var started = await adapter.CreateOperationAsync(state, User, default);

        Assert.True(started.Succeeded, started.Error);
        await using var operation = started.Operation!;
        for (var index = 0; index < 10; index++)
        {
            var result = await operation.EvaluateAsync(
                new FieldAddress("dc1", "row_new", index % 2 == 0 ? "qty" : "rate"),
                index + 2m,
                default);
            Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        }

        var completed = await operation.CompleteAsync(default);

        Assert.True(completed.Succeeded, completed.Errors.FirstOrDefault()?.Message);
        Assert.Equal(1, factory.CallCount);
        Assert.Equal(1, factory.LastCreatedRuntime!.HydrationCount);
        Assert.Equal(1, factory.LastCreatedRuntime.ReadbackCount);
        Assert.Equal(10m, state.DataContainers["dc1"].Rows[0].Fields["qty"].Value);
        Assert.Equal(11m, state.DataContainers["dc1"].Rows[0].Fields["rate"].Value);
        Assert.Equal(110m, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Rebuild_after_rejection_replays_only_accepted_state()
    {
        var state = CreateState();
        var factory = new LegacyRuntimeFactory();
        var adapter = new AxpertFieldEngine(factory);
        var started = await adapter.CreateOperationAsync(state, User, default);

        Assert.True(started.Succeeded, started.Error);
        await using var operation = started.Operation!;
        Assert.False((await operation.EvaluateAsync(
            new FieldAddress("dc1", "row_new", "qty"), "invalid", default)).Succeeded);
        Assert.True((await operation.RebuildAsync(default)).Succeeded);
        Assert.True((await operation.EvaluateAsync(
            new FieldAddress("dc1", "row_new", "rate"), 20m, default)).Succeeded);
        var completed = await operation.CompleteAsync(default);

        Assert.True(completed.Succeeded, completed.Errors.FirstOrDefault()?.Message);
        Assert.Equal(2, factory.CallCount);
        Assert.Equal([1, 1], factory.CreatedRuntimes.Select(runtime => runtime.HydrationCount));
        Assert.Equal([0, 1], factory.CreatedRuntimes.Select(runtime => runtime.ReadbackCount));
        Assert.Equal(1m, state.DataContainers["dc1"].Rows[0].Fields["qty"].Value);
        Assert.Equal(20m, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Hydration_and_readback_preserve_normalized_source_ids()
    {
        var state = CreateState();
        state.DataContainers["dc1"].Rows[0].Fields["qty"] = new AxFieldValue(1m, sourceId: 501);
        var factory = new LegacyRuntimeFactory(sourceKeyField: true);
        var adapter = new AxpertFieldEngine(factory);
        var started = await adapter.CreateOperationAsync(state, User, default);

        Assert.True(started.Succeeded, started.Error);
        await using var operation = started.Operation!;
        Assert.Equal(501, factory.LastCreatedRuntime!.Validate._sdef.GetIDValue("qty", 1));
        var completed = await operation.CompleteAsync(default);

        Assert.True(completed.Succeeded, completed.Errors.FirstOrDefault()?.Message);
        Assert.Equal(501, state.DataContainers["dc1"].Rows[0].Fields["qty"].SourceId);
        Assert.Equal(501, state.DataContainers["dc1"].Rows[0].Fields["qty"].OriginalSourceId);
    }

    [Fact]
    public async Task Global_gate_is_held_for_the_whole_operation()
    {
        var factory = new LegacyRuntimeFactory();
        var adapter = new AxpertFieldEngine(factory);
        var first = await adapter.CreateOperationAsync(CreateState(), User, default);

        Assert.True(first.Succeeded, first.Error);
        var secondState = CreateState();
        var secondUser = User with { TenantId = "other-tenant" };
        secondState = AxDataState.Rehydrate(
            "object-2",
            secondUser.TenantId,
            secondState.TransId,
            secondState.KeyField,
            secondState.KeyValue,
            secondState.RecordId,
            secondState.OwnerUserName,
            secondState.InitiatorUserName,
            secondState.Version,
            secondState.StructDefVersion,
            secondState.CreatedAtUtc,
            secondState.LastAccessedAtUtc,
            secondState.ExpiresAtUtc,
            secondState.Status,
            secondState.DataContainers.Values,
            secondState.Workflow,
            secondState.Files,
            secondState.AxOpen);
        var secondTask = adapter.CreateOperationAsync(secondState, secondUser, default);

        await Task.Delay(50);
        Assert.False(secondTask.IsCompleted);
        Assert.Equal(1, factory.CallCount);

        await first.Operation!.DisposeAsync();
        var second = await secondTask;
        Assert.True(second.Succeeded, second.Error);
        Assert.Equal(2, factory.CallCount);
        await second.Operation!.DisposeAsync();
    }

    [Fact]
    public async Task Runtime_preparation_restores_session_globals_and_prepares_expressions()
    {
        var runtime = LegacyFixture.CreateRuntime();
        runtime.Validate._common._globalVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenant_rate"] = "7"
        };
        runtime.Validate._common._sessionVars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            [Constants.SESSION_DATA.USERNAME.ToString()] = "owner"
        };
        runtime.Validate._common.ApiDateFormat = "yyyy-MM-dd";

        await AxpertFieldRuntimeFactory.PrepareValidationAsync(
            runtime.Validate,
            runtime.Validate._common,
            42,
            CancellationToken.None);

        Assert.Equal("7", runtime.Validate._parser.GetVarValue("tenant_rate"));
        Assert.Equal("42", runtime.Validate._parser.GetVarValue("recordid"));
        Assert.Equal("0", runtime.Validate._parser.GetVarValue("qty"));
        Assert.Equal(42, runtime.Validate._sdef.LastSavedRecordid);
        Assert.Equal("yyyy-MM-dd", runtime.Validate._sdef.clientDateFormat);
    }

    [Fact]
    public async Task Valid_dependency_matches_existing_validate_and_parser_result()
    {
        var direct = LegacyFixture.CreateRuntime();
        LegacyFixture.Seed(direct, 3m, 12.50m, 12.50m);
        var legacyResult = await direct.Validate.ValidateData();
        var legacyAmount = direct.Validate._sdef.GetFieldValue("amount", 1);

        Assert.True(
            string.Equals("Success", legacyResult.Result, StringComparison.Ordinal),
            legacyResult.Message);
        Assert.False(string.IsNullOrEmpty(legacyAmount), legacyResult.Message);
        var expected = decimal.Parse(legacyAmount, System.Globalization.CultureInfo.InvariantCulture);

        var state = CreateState(amountIsReadOnly: true);
        var adapter = new AxpertFieldEngine(new LegacyRuntimeFactory());
        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_new", "qty"),
            3m,
            User,
            default);

        Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        Assert.Equal(37.50m, expected);
        var dependency = Assert.Single(result.Changes, change => change.Source == FieldChangeSource.Dependency);
        Assert.Equal(new FieldAddress("dc1", "row_new", "amount"), dependency.Address);
        Assert.Equal(expected, dependency.Value);
        Assert.Equal(expected, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
        Assert.True(state.DataContainers["dc1"].Rows[0].Fields["amount"].IsReadOnly);
    }

    [Fact]
    public async Task Invalid_numeric_value_matches_existing_validation_and_does_not_mutate_state()
    {
        var direct = LegacyFixture.CreateRuntime();
        LegacyFixture.Seed(direct, "not-a-number", 12.50m, 12.50m);
        var legacyResult = await direct.Validate.ValidateData();

        var state = CreateState();
        var adapter = new AxpertFieldEngine(new LegacyRuntimeFactory());
        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_new", "qty"),
            "not-a-number",
            User,
            default);

        Assert.Equal("Fail", legacyResult.Result);
        Assert.False(result.Succeeded);
        Assert.Empty(result.Changes);
        Assert.Equal(1m, state.DataContainers["dc1"].Rows[0].Fields["qty"].Value);
        Assert.Equal(12.50m, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Calculation_matches_direct_parser_result()
    {
        var runtime = LegacyFixture.CreateRuntime();
        runtime.Validate._parser.RegisterVar("qty", "n", "4");
        runtime.Validate._parser.RegisterVar("rate", "n", "12.5");
        var parsed = runtime.Validate._parser.Evaluate("qty * rate");
        var expected = decimal.Parse(
            runtime.Validate._parser.Value,
            System.Globalization.CultureInfo.InvariantCulture);

        var state = CreateState();
        var adapter = new AxpertFieldEngine(new LegacyRuntimeFactory());
        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_new", "qty"),
            4m,
            User,
            default);

        Assert.True(parsed);
        Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        Assert.Equal(50m, expected);
        Assert.Equal(expected, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public async Task Existing_record_identity_is_restored_before_validation()
    {
        const long recordId = 1001;
        var state = CreateState(recordId: recordId);
        var factory = new LegacyRuntimeFactory();
        var adapter = new AxpertFieldEngine(factory);

        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_existing", "qty"),
            3m,
            User,
            default);

        Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        var runtime = Assert.IsType<AxpertFieldRuntime>(factory.LastCreatedRuntime);
        Assert.Equal(recordId, runtime.Validate._sdef.LastSavedRecordid);
        Assert.Equal(
            recordId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            runtime.Validate._parser.GetVarValue("recordid"));
        Assert.Equal(recordId, runtime.Validate._sdef.GetRecordId(1, 1));
        var fields = Assert.IsAssignableFrom<IEnumerable<TField>>(runtime.Validate._sdef.Flds);
        Assert.All(
            fields,
            field => Assert.Equal(recordId, runtime.Validate._sdef.GetRecordId(field, 1)));
    }

    [Fact]
    public async Task Reordered_rows_map_current_array_position_to_legacy_row_number()
    {
        var fields = new Dictionary<string, AxFieldValue>
        {
            ["qty"] = new(1m),
            ["rate"] = new(2m),
            ["amount"] = new(2m)
        };
        var state = new AxDataState(
            "object-1", "sample-tenant", "sample-order", "owner", "owner", "1", DateTimeOffset.UnixEpoch);
        state.DataContainers.Add("dc1", new AxDataContainer("dc1", true,
        [
            new AxDataRow("row_b", AxRowState.Unchanged, 202, fields),
            new AxDataRow("row_a", AxRowState.Unchanged, 101, fields.ToDictionary(pair => pair.Key, pair => new AxFieldValue(pair.Value.Value)))
        ]));
        var factory = new LegacyRuntimeFactory(asGrid: true);
        var adapter = new AxpertFieldEngine(factory);

        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_a", "qty"),
            3m,
            User,
            default);

        Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        var runtime = Assert.IsType<AxpertFieldRuntime>(factory.LastCreatedRuntime);
        Assert.Equal(202, runtime.Validate._sdef.GetRecordId(1, 1));
        Assert.Equal(101, runtime.Validate._sdef.GetRecordId(1, 2));
    }

    [Fact]
    public void Sparse_grid_submission_uses_highest_submitted_row_number()
    {
        var runtime = LegacyFixture.CreateRuntime(asGrid: true);

        LegacyFixture.SeedRow(runtime, 3, 5m, 7m, 35m);

        Assert.Equal(3, Assert.Single(runtime.Validate._sdef.Frames!).RowCount);
        Assert.Equal("5", runtime.Validate._sdef.GetFieldValue("qty", 3));
    }

    [Fact]
    public async Task Grid_validation_registers_each_rows_values_before_calculation()
    {
        var runtime = LegacyFixture.CreateRuntime(asGrid: true);
        LegacyFixture.SeedRow(runtime, 1, 2m, 3m, 0m);
        LegacyFixture.SeedRow(runtime, 2, 5m, 7m, 0m);

        var result = await runtime.Validate.ValidateData();

        Assert.Equal("Success", result.Result);
        Assert.Equal("6.00", runtime.Validate._sdef.GetFieldValue("amount", 1));
        Assert.Equal("35.00", runtime.Validate._sdef.GetFieldValue("amount", 2));
    }

    [Fact]
    public async Task New_record_identity_remains_zero_before_validation()
    {
        var state = CreateState();
        var factory = new LegacyRuntimeFactory();
        var adapter = new AxpertFieldEngine(factory);

        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_new", "qty"),
            3m,
            User,
            default);

        Assert.True(result.Succeeded, result.Errors.FirstOrDefault()?.Message);
        var runtime = Assert.IsType<AxpertFieldRuntime>(factory.LastCreatedRuntime);
        Assert.Equal(0, runtime.Validate._sdef.LastSavedRecordid);
        Assert.Equal("0", runtime.Validate._parser.GetVarValue("recordid"));
        Assert.Equal(0, runtime.Validate._sdef.GetRecordId(1, 1));
        var fields = Assert.IsAssignableFrom<IEnumerable<TField>>(runtime.Validate._sdef.Flds);
        Assert.All(
            fields,
            field => Assert.Equal(0, runtime.Validate._sdef.GetRecordId(field, 1)));
    }

    [Fact]
    public async Task Failed_validation_expression_is_not_allowed_to_read_parser_value()
    {
        var runtime = LegacyFixture.CreateRuntime(invalidValidationExpression: true);
        LegacyFixture.Seed(runtime, 3m, 12.50m, 12.50m);

        Assert.True(runtime.Validate._parser.Evaluate("1 + 1"));
        Assert.Equal("2", runtime.Validate._parser.Value);

        var result = await runtime.Validate.ValidateData();

        Assert.Equal("Fail", result.Result);
    }

    [Fact]
    public async Task Failed_calculation_does_not_return_or_apply_the_previous_value()
    {
        var state = CreateState();
        var adapter = new AxpertFieldEngine(new LegacyRuntimeFactory(invalidCalculationExpression: true));

        var result = await adapter.SetValidateAndRefreshAsync(
            state,
            new FieldAddress("dc1", "row_new", "qty"),
            3m,
            User,
            default);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Changes);
        Assert.Equal(1m, state.DataContainers["dc1"].Rows[0].Fields["qty"].Value);
        Assert.Equal(12.50m, state.DataContainers["dc1"].Rows[0].Fields["amount"].Value);
    }

    [Fact]
    public void Failed_tax_formula_throws_instead_of_becoming_zero()
    {
        var runtime = LegacyFixture.CreateRuntime();
        var helper = LegacyFixture.GetHelper(runtime.Validate);
        var table = new DataTable();
        table.Columns.Add("seed");
        table.Columns.Add("taxcode");
        table.Columns.Add("basicamount");
        table.Columns.Add("rate");
        table.Columns.Add("formula");
        table.Rows.Add("x total", "tax1", "100", "5", "missing_variable + 1");

        var error = Assert.Throws<InvalidOperationException>(() => helper.TaxCompute(
            runtime.Validate._sdef.GetField("amount")!,
            1,
            new SQLResult { data = table }));

        Assert.Equal("Tax formula evaluation failed.", error.Message);
    }

    [Fact]
    public void Submit_value_overloads_confirm_plaintext_and_encrypted_directions()
    {
        var runtime = LegacyFixture.CreateRuntime(encryptedField: true);
        var structDef = runtime.Validate._sdef;

        structDef.SubmitValue("secret", 1, "plain-text", "", 0, 0, 0);
        var encrypted = structDef.GetFieldValue("secret", 1);
        structDef.SubmitValue("secret", 1, encrypted);

        Assert.NotEqual("plain-text", encrypted);
        Assert.Equal("plain-text", structDef.GetFieldValue("secret", 1));
    }

    private static AxDataState CreateState(bool amountIsReadOnly = false, long? recordId = null)
    {
        var state = new AxDataState(
            "object-1",
            "sample-tenant",
            "sample-order",
            "owner",
            "owner",
            "1",
            DateTimeOffset.UnixEpoch)
        {
            RecordId = recordId
        };
        state.DataContainers.Add(
            "dc1",
            new AxDataContainer(
                "dc1",
                false,
                [new AxDataRow(
                    recordId is null ? "row_new" : "row_existing",
                    recordId is null ? AxRowState.New : AxRowState.Unchanged,
                    recordId,
                    new Dictionary<string, AxFieldValue>
                {
                    ["qty"] = new(1m),
                    ["rate"] = new(12.50m),
                    ["amount"] = new(12.50m, amountIsReadOnly)
                })]));
        return state;
    }

    private sealed class LegacyRuntimeFactory(
        bool invalidCalculationExpression = false,
        bool asGrid = false,
        bool sourceKeyField = false) : IAxpertFieldRuntimeFactory
    {
        public int CallCount { get; private set; }

        public AxpertFieldRuntime? LastCreatedRuntime { get; private set; }

        public List<AxpertFieldRuntime> CreatedRuntimes { get; } = [];

        public Task<AxpertOperationResult<AxpertFieldRuntime>> CreateAsync(
            AxpertFieldRuntimeRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastCreatedRuntime = LegacyFixture.CreateRuntime(
                invalidCalculationExpression: invalidCalculationExpression,
                asGrid: asGrid,
                sourceKeyField: sourceKeyField);
            CreatedRuntimes.Add(LastCreatedRuntime);
            return Task.FromResult(AxpertOperationResult<AxpertFieldRuntime>.Success(
                LastCreatedRuntime));
        }
    }

    private static class LegacyFixture
    {
        private static readonly FieldInfo ValidateHelperField = typeof(Validate).GetField(
            "_validateHelper",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Validate helper field was not found.");

        public static AxpertFieldRuntime CreateRuntime(
            bool invalidValidationExpression = false,
            bool invalidCalculationExpression = false,
            bool encryptedField = false,
            bool asGrid = false,
            bool sourceKeyField = false)
        {
            var common = new Utils();
            var trace = new NullTraceService();
            var structDef = new StructDef();
            structDef.Struct!.TransId = "sample-order";
            structDef.Frames!.Add(new TFrame
            {
                FrameNo = 1,
                StartIndex = 0,
                FieldCount = encryptedField ? 4 : 3,
                RowCount = 1,
                TableName = "lines",
                AsGrid = asGrid
            });

            structDef.Flds!.AddRange(
            [
                NumericField(
                    "qty",
                    1,
                    invalidValidationExpression ? "missing_variable + 1" : "",
                    asGrid: asGrid,
                    sourceKey: sourceKeyField),
                NumericField("rate", 2, asGrid: asGrid),
                NumericField("amount", 3, calculation: invalidCalculationExpression ? "missing_variable + 1" : "qty * rate", asGrid: asGrid)
            ]);
            if (encryptedField)
            {
                structDef.Flds!.Add(new TField
                {
                    FieldName = "secret",
                    Caption = "Secret",
                    Tablename = "lines",
                    DataType = "c",
                    ModeofEntry = "accept",
                    FrameNo = 1,
                    OrderNo = 4,
                    AsGrid = asGrid,
                    SaveValue = true,
                    EncryptValue = true
                });
            }

            var parser = new AxParser(common, structDef);
            var validate = new Validate(common, trace)
            {
                _common = common,
                _sdef = structDef,
                _parser = parser
            };
            var helper = new ValidateHelper(
                "",
                null!,
                parser,
                structDef,
                new AxStructDefUtils(common),
                null!,
                trace,
                common);
            ValidateHelperField.SetValue(validate, helper);

            return new AxpertFieldRuntime(
                validate,
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["dc1"] = 1 });
        }

        public static ValidateHelper GetHelper(Validate validate) =>
            (ValidateHelper)(ValidateHelperField.GetValue(validate)
                ?? throw new InvalidOperationException("Validate helper was not initialized."));

        public static void Seed(AxpertFieldRuntime runtime, object qty, object rate, object amount)
        {
            SeedRow(runtime, 1, qty, rate, amount);
        }

        public static void SeedRow(
            AxpertFieldRuntime runtime,
            int rowNumber,
            object qty,
            object rate,
            object amount)
        {
            Submit(runtime.Validate, "qty", rowNumber, qty);
            Submit(runtime.Validate, "rate", rowNumber, rate);
            Submit(runtime.Validate, "amount", rowNumber, amount);
        }

        private static void Submit(Validate validate, string name, int rowNumber, object value)
        {
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            validate._sdef.SubmitValue(name, rowNumber, text, "", 0, 0, 0);
            validate._parser.RegisterVar(name, "n", text);
        }

        private static TField NumericField(
            string name,
            int order,
            string validation = "",
            string calculation = "",
            bool asGrid = false,
            bool sourceKey = false) => new()
        {
            FieldName = name,
            Caption = name,
            Tablename = "lines",
            DataType = "n",
            ModeofEntry = string.IsNullOrEmpty(calculation) ? "accept" : "calculate",
            FrameNo = 1,
            OrderNo = order,
            AsGrid = asGrid,
            SourceKey = sourceKey,
            Width = 20,
            Dec = 2,
            CurDec = 2,
            Cexp = calculation,
            Cvalexp = validation,
            Exprn = string.IsNullOrEmpty(calculation) ? -1 : 0,
            ValExprn = string.IsNullOrEmpty(validation) ? -1 : 0
        };
    }

    private sealed class NullTraceService : IARMLogTraceService
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
        public void LogDataTable(DataTable dataTable, DateTime? startTime = null, DateTime? endTime = null) { }
        public string GetTraceLog() => "";
        public Task<string> GetTraceFile(string fileName) => Task.FromResult("");
        public void Clear() { }
    }
}
