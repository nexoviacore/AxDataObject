using System.Globalization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.FillGrid;

namespace AxData.Integration.Axpert.Development;

public sealed record M1DevelopmentAxpertOptions
{
    public string TenantId { get; init; } = "sample-tenant";

    public string TransId { get; init; } = "sample-order";

    public string StructDefVersion { get; init; } = "1";
}

public sealed class M1DevelopmentStructDefProvider(M1DevelopmentAxpertOptions options) : IStructDefProvider
{
    private readonly StructDefHandle _definition = CreateDefinition(options);

    public Task<StructDefHandle?> GetAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<StructDefHandle?>(Matches(tenantId, transId, options.StructDefVersion)
            ? _definition
            : null);
    }

    public Task<StructDefHandle?> GetVersionAsync(
        string tenantId,
        string transId,
        string version,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<StructDefHandle?>(Matches(tenantId, transId, version)
            ? _definition
            : null);
    }

    private bool Matches(string tenantId, string transId, string version) =>
        string.Equals(tenantId, options.TenantId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(transId, options.TransId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(version, options.StructDefVersion, StringComparison.Ordinal);

    private static StructDefHandle CreateDefinition(M1DevelopmentAxpertOptions settings) => new(
        settings.TenantId,
        settings.TransId,
        settings.StructDefVersion,
        [
            new DataContainerDefinition(
                "dc1",
                false,
                [new FieldDefinition("customer")]),
            new DataContainerDefinition(
                "dc2",
                true,
                [
                    new FieldDefinition("item"),
                    new FieldDefinition("qty"),
                    new FieldDefinition("rate"),
                    new FieldDefinition("amount")
                ],
                ["item", "rate", "qty"])
        ],
        [new FieldDependency("dc2", "qty", "dc2", "amount", "qty-times-rate")],
        []);
}

public sealed class M1DevelopmentAuthorizationProvider(M1DevelopmentAxpertOptions options)
    : IAxpertAuthorizationProvider
{
    private static readonly IReadOnlySet<string> EmptySet =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> EmptyFieldSets =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReadOnlyFieldSets =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["dc2"] = new HashSet<string>(["amount"], StringComparer.OrdinalIgnoreCase)
        };

    private static readonly IReadOnlySet<string> AllowedOperations =
        new HashSet<string>(["query", "set"], StringComparer.OrdinalIgnoreCase);

    public Task<AxCreatePermission> GetCreatePermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AxCreatePermission(IsKnown(tenantId, transId, user)));
    }

    public Task<AxQueryPermission> GetQueryPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken) =>
        GetPermissionAsync(tenantId, transId, user, cancellationToken);

    public Task<AxQueryPermission> GetSetPermissionAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken) =>
        GetPermissionAsync(tenantId, transId, user, cancellationToken);

    private Task<AxQueryPermission> GetPermissionAsync(
        string tenantId,
        string transId,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsKnown(tenantId, transId, user))
        {
            return Task.FromResult(AxQueryPermission.Denied);
        }

        return Task.FromResult(new AxQueryPermission(
            IsAllowed: true,
            CanEditTransaction: true,
            EmptySet,
            EmptySet,
            EmptyFieldSets,
            EmptyFieldSets,
            EmptySet,
            ReadOnlyFieldSets,
            AllowedOperations));
    }

    private bool IsKnown(string tenantId, string transId, AxUserContext user) =>
        string.Equals(tenantId, options.TenantId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(transId, options.TransId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(user.TenantId, options.TenantId, StringComparison.OrdinalIgnoreCase);
}

public sealed class M1DevelopmentDataLoader(IRowIdGenerator rowIdGenerator) : IAxpertDataLoader
{
    public Task<AxDataLoadResult> LoadAsync(
        StructDefHandle definition,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = new AxDataState(
            "m1-placeholder",
            user.TenantId,
            definition.TransId,
            user.UserName,
            user.UserName,
            definition.Version,
            DateTimeOffset.UnixEpoch)
        {
            KeyField = key.KeyField,
            KeyValue = key.KeyValue,
            RecordId = key.RecordId
        };

        state.DataContainers.Add(
            "dc1",
            new AxDataContainer(
                "dc1",
                false,
                [new AxDataRow(rowIdGenerator.NewRowId(), AxRowState.Unchanged, 1, new Dictionary<string, AxFieldValue>
                {
                    ["customer"] = new("M1 customer")
                })]));
        state.DataContainers.Add("dc2", new AxDataContainer("dc2", true));
        return Task.FromResult(new AxDataLoadResult(true, state, null));
    }
}

public sealed class M1DevelopmentFieldEngine : IAxpertFieldEngine
{
    public Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        AxDataState state,
        FieldAddress field,
        object? value,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(state.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase) ||
            !TryGetField(state, field, out var target))
        {
            return Task.FromResult(FieldEvaluationResult.Failure(
                new FieldEvaluationError(field, "FIELD_NOT_FOUND", "The M1 field address is invalid.")));
        }

        target.SetValue(value);
        var changes = new List<FieldChange> { new(field, value, FieldChangeSource.Submitted) };
        if (string.Equals(field.DataContainer, "dc2", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(field.FieldName, "qty", StringComparison.OrdinalIgnoreCase) &&
            TryGetField(state, new FieldAddress("dc2", field.RowId, "rate"), out var rateField) &&
            TryGetField(state, new FieldAddress("dc2", field.RowId, "amount"), out var amountField) &&
            TryDecimal(value, out var quantity) &&
            TryDecimal(rateField.Value, out var rate))
        {
            var amount = quantity * rate;
            amountField.SetValue(amount);
            changes.Add(new FieldChange(
                new FieldAddress("dc2", field.RowId, "amount"),
                amount,
                FieldChangeSource.Dependency));
        }

        return Task.FromResult(new FieldEvaluationResult(true, changes, []));
    }

    private static bool TryGetField(AxDataState state, FieldAddress address, out AxFieldValue field)
    {
        field = null!;
        var row = state.DataContainers.TryGetValue(address.DataContainer, out var container)
            ? container.Rows.FirstOrDefault(candidate =>
                string.Equals(candidate.RowId, address.RowId, StringComparison.OrdinalIgnoreCase))
            : null;
        return row is not null && row.Fields.TryGetValue(address.FieldName, out field!);
    }

    private static bool TryDecimal(object? value, out decimal result) =>
        decimal.TryParse(
            Convert.ToString(value, CultureInfo.InvariantCulture),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
}

public sealed class M1DevelopmentFillGridRuntimeFactory : IAxpertFillGridRuntimeFactory
{
    public Task<AxpertOperationResult<AxpertFillGridRuntime>> CreateAsync(
        AxpertFillGridRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AxpertOperationResult<AxpertFillGridRuntime>.Success(
            new AxpertFillGridRuntime([], new NoOpExecutor())));
    }

    private sealed class NoOpExecutor : IAxpertFillGridQueryExecutor
    {
        public Task<AxpertOperationResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ExecuteAsync(
            AxpertFillGridQuery query,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The M1 fixture does not define FillGrid metadata.");
    }
}
