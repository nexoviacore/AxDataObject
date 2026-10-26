using System.Globalization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Model;

namespace AxData.TestSupport.Fakes;

public sealed class FakeStructDefProvider(params StructDefHandle[] definitions) : IStructDefProvider
{
    private readonly Dictionary<(string TenantId, string TransId, string Version), StructDefHandle> _definitions = definitions
        .ToDictionary(
            definition => (Normalize(definition.TenantId), Normalize(definition.TransId), Normalize(definition.Version)),
            definition => definition);

    private readonly Dictionary<(string TenantId, string TransId), StructDefHandle> _latestDefinitions = definitions
        .GroupBy(definition => (Normalize(definition.TenantId), Normalize(definition.TransId)))
        .ToDictionary(group => group.Key, group => group.Last());

    public int CallCount { get; private set; }

    public Task<StructDefHandle?> GetAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        _latestDefinitions.TryGetValue((Normalize(tenantId), Normalize(transId)), out var definition);
        return Task.FromResult(definition);
    }

    public Task<StructDefHandle?> GetVersionAsync(
        string tenantId,
        string transId,
        string version,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        _definitions.TryGetValue(
            (Normalize(tenantId), Normalize(transId), Normalize(version)),
            out var definition);
        return Task.FromResult(definition);
    }

    private static string Normalize(string value) => value.ToUpperInvariant();
}

public sealed class FakeAxpertAuthorizationProvider : IAxpertAuthorizationProvider
{
    public AxCreatePermission Permission { get; set; } = new(true);

    public AxQueryPermission QueryPermission { get; set; } = CreateQueryPermission();

    public AxQueryPermission SetPermission { get; set; } = CreateQueryPermission();

    public int CallCount { get; private set; }

    public int QueryCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public AxRecordKey? LastQueryKey { get; private set; }

    public AxRecordKey? LastSetKey { get; private set; }

    public Task<AxCreatePermission> GetCreatePermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        return Task.FromResult(Permission);
    }

    public Task<AxQueryPermission> GetQueryPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        QueryCallCount++;
        LastQueryKey = key;
        return Task.FromResult(QueryPermission);
    }

    public Task<AxQueryPermission> GetSetPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SetCallCount++;
        LastSetKey = key;
        return Task.FromResult(SetPermission);
    }

    public static AxQueryPermission CreateQueryPermission(
        bool isAllowed = true,
        bool canEditTransaction = true,
        IReadOnlySet<string>? includedDataContainers = null,
        IReadOnlySet<string>? excludedDataContainers = null,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? includedFields = null,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? excludedFields = null,
        IReadOnlySet<string>? readOnlyDataContainers = null,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? readOnlyFields = null,
        IReadOnlySet<string>? allowedOperations = null) => new(
            isAllowed,
            canEditTransaction,
            includedDataContainers ?? Set(),
            excludedDataContainers ?? Set(),
            includedFields ?? FieldSets(),
            excludedFields ?? FieldSets(),
            readOnlyDataContainers ?? Set(),
            readOnlyFields ?? FieldSets(),
            allowedOperations ?? Set("query", "set", "save", "recall"));

    public static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> FieldSets(
        params (string DataContainer, string[] Fields)[] values) =>
        values.ToDictionary(
            value => value.DataContainer,
            value => (IReadOnlySet<string>)Set(value.Fields),
            StringComparer.OrdinalIgnoreCase);
}

public sealed class FakeAxpertDataLoader : IAxpertDataLoader
{
    public AxDataLoadResult Result { get; set; } = new(false, null, "No load result was scripted.");

    public int CallCount { get; private set; }

    public AxRecordKey? LastKey { get; private set; }

    public Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        LastKey = key;
        return Task.FromResult(Result);
    }
}

public sealed class FakeAxpertFieldEngine(StructDefHandle definition) : IAxpertFieldEngine
{
    public List<FieldAddress> EvaluatedFields { get; } = [];

    public int OperationCount { get; private set; }

    public int RebuildCount { get; private set; }

    public int CompleteCount { get; private set; }

    public Task<FieldOperationStartResult> CreateOperationAsync(
        AxDataState state,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OperationCount++;
        if (!string.Equals(state.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FieldOperationStartResult.Failure(
                "The user and state tenant do not match."));
        }

        return Task.FromResult(FieldOperationStartResult.Success(new Operation(this, state)));
    }

    private Task<FieldEvaluationResult> EvaluateAsync(
        AxDataState state,
        FieldAddress field,
        object? value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EvaluatedFields.Add(field);

        if (!TryGetField(state, field, out var target))
        {
            return Task.FromResult(FieldEvaluationResult.Failure(
                new(field, "FIELD_NOT_FOUND", "The scripted field address was not found.")));
        }

        if (!TryDecimal(value, out var submittedValue))
        {
            return Task.FromResult(FieldEvaluationResult.Failure(
                new(field, "INVALID_NUMBER", "The scripted field accepts decimal values only.")));
        }

        target.SetValue(submittedValue);
        var changes = new List<FieldChange>
        {
            new(field, submittedValue, FieldChangeSource.Submitted)
        };

        foreach (var dependency in definition.Dependencies.Where(dependency =>
                     string.Equals(dependency.SourceContainer, field.DataContainer, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(dependency.SourceField, field.FieldName, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.Equals(dependency.ScriptName, "qty-times-rate", StringComparison.Ordinal))
            {
                continue;
            }

            var quantityAddress = new FieldAddress(field.DataContainer, field.RowId, "qty");
            var rateAddress = new FieldAddress(field.DataContainer, field.RowId, "rate");
            var amountAddress = new FieldAddress(dependency.TargetContainer, field.RowId, dependency.TargetField);

            if (!TryGetField(state, quantityAddress, out var quantityField) ||
                !TryGetField(state, rateAddress, out var rateField) ||
                !TryGetField(state, amountAddress, out var amountField) ||
                !TryDecimal(quantityField.Value, out var quantity) ||
                !TryDecimal(rateField.Value, out var rate))
            {
                return Task.FromResult(FieldEvaluationResult.Failure(
                    new(field, "DEPENDENCY_INPUT_INVALID", "The scripted dependency inputs are invalid.")));
            }

            var amount = quantity * rate;
            amountField.SetValue(amount);
            changes.Add(new(amountAddress, amount, FieldChangeSource.Dependency));
        }

        return Task.FromResult(new FieldEvaluationResult(true, changes, []));
    }

    private sealed class Operation(FakeAxpertFieldEngine owner, AxDataState state) : IAxpertFieldOperation
    {
        private readonly List<FieldChange> _changes = [];

        public async Task<FieldEvaluationResult> EvaluateAsync(
            FieldAddress field,
            object? value,
            CancellationToken cancellationToken)
        {
            var result = await owner.EvaluateAsync(state, field, value, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                _changes.AddRange(result.Changes);
            }

            return result;
        }

        public Task<FieldOperationStatus> RebuildAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RebuildCount++;
            return Task.FromResult(FieldOperationStatus.Success);
        }

        public Task<FieldEvaluationResult> CompleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.CompleteCount++;
            return Task.FromResult(new FieldEvaluationResult(true, _changes.ToArray(), []));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static bool TryGetField(AxDataState state, FieldAddress address, out AxFieldValue field)
    {
        field = null!;
        if (!state.DataContainers.TryGetValue(address.DataContainer, out var container) ||
            container.Rows.FirstOrDefault(row => string.Equals(row.RowId, address.RowId, StringComparison.OrdinalIgnoreCase)) is not { } row ||
            !row.Fields.TryGetValue(address.FieldName, out var found))
        {
            return false;
        }

        field = found;
        return true;
    }

    private static bool TryDecimal(object? value, out decimal result)
    {
        if (value is decimal decimalValue)
        {
            result = decimalValue;
            return true;
        }

        return decimal.TryParse(
            Convert.ToString(value, CultureInfo.InvariantCulture),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }
}

public sealed class FakeAxpertFillGridEngine : IAxpertFillGridEngine
{
    public FillGridExecutionResult Result { get; set; } = new(true, []);

    public int CallCount { get; private set; }

    public IReadOnlyCollection<FieldAddress> LastChangedParents { get; private set; } = [];

    public Task<FillGridExecutionResult> ExecuteApplicableAsync(
        AxDataState state,
        IReadOnlyCollection<FieldAddress> changedParents,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        LastChangedParents = changedParents.ToArray();
        return Task.FromResult(Result);
    }
}

public sealed class FakeAxpertTransactionWriter : IAxpertTransactionWriter
{
    public AxSaveResult Result { get; set; } = new(false, null, "No save result was scripted.");

    public Task<AxSaveResult> SaveAsync(
        AxDataState state,
        AxSaveContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result);
    }
}
