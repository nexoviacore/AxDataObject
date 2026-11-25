using AxData.Domain.Model;
using AxData.Application.Ports;

namespace AxData.Application.Ports.Axpert;

public sealed record StructDefHandle(
    string TenantId,
    string TransId,
    IReadOnlyList<DataContainerDefinition> DataContainers,
    IReadOnlyList<FieldDependency> Dependencies,
    IReadOnlyList<FillGridDefinition> FillGrids,
    bool RequiresCompatibilityFallback = false,
    IReadOnlyList<FieldDependencyDiagnostic>? DependencyDiagnostics = null);

public sealed record DataContainerDefinition(
    string Name,
    bool IsGrid,
    IReadOnlyList<FieldDefinition> Fields,
    IReadOnlyList<string>? EvaluationOrder = null);

public sealed record FieldDefinition(string Name, bool IsReadOnly = false, bool SourceKey = false);

public sealed record FieldDependency(
    string SourceContainer,
    string SourceField,
    string? TargetContainer,
    string TargetField,
    FieldDependencyKind Kind = FieldDependencyKind.Expression,
    int DeclarationOrder = 0,
    int SourceFrameNo = 0,
    int? TargetFrameNo = null,
    bool SourceIsGrid = false,
    bool TargetIsGrid = false,
    bool TargetIsPopup = false,
    bool IsResolved = true);

public enum FieldDependencyKind
{
    Expression,
    Select,
    Fill,
    Aggregate,
    FillGrid,
    DataContainer,
    Label,
    DependentFrame,
    Grid
}

public sealed record FieldDependencyDiagnostic(
    FieldDependencyDiagnosticKind Kind,
    string Source,
    string Target,
    string Message);

public enum FieldDependencyDiagnosticKind
{
    MalformedMetadata,
    UnresolvedReference,
    Cycle
}

public sealed record FillGridDefinition(
    string Name,
    string TargetContainer,
    IReadOnlyList<string> ParentFields,
    FillGridMode Mode);

public enum FillGridMode
{
    Append,
    Replace
}

public sealed record FieldAddress(string DataContainer, string RowId, string FieldName);

public sealed record FieldChange(FieldAddress Address, object? Value, FieldChangeSource Source);

public enum FieldChangeSource
{
    Submitted,
    Dependency,
    FillGrid
}

public sealed record FieldEvaluationError(FieldAddress Address, string Code, string Message);

public sealed record FieldEvaluationResult(
    bool Succeeded,
    IReadOnlyList<FieldChange> Changes,
    IReadOnlyList<FieldEvaluationError> Errors)
{
    public static FieldEvaluationResult Success(params FieldChange[] changes) =>
        new(true, changes, []);

    public static FieldEvaluationResult Failure(FieldEvaluationError error) =>
        new(false, [], [error]);
}

public sealed record FieldOperationStartResult(
    bool Succeeded,
    IAxpertFieldOperation? Operation,
    string? Error = null,
    FieldOperationFailureKind FailureKind = FieldOperationFailureKind.DependencyUnavailable)
{
    public static FieldOperationStartResult Success(IAxpertFieldOperation operation) =>
        new(true, operation);

    public static FieldOperationStartResult Failure(
        string error,
        FieldOperationFailureKind failureKind = FieldOperationFailureKind.DependencyUnavailable) =>
        new(false, null, error, failureKind);
}

public enum FieldOperationFailureKind
{
    DependencyUnavailable
}

public sealed record FieldOperationStatus(
    bool Succeeded,
    string? Error = null,
    FieldOperationFailureKind FailureKind = FieldOperationFailureKind.DependencyUnavailable)
{
    public static FieldOperationStatus Success { get; } = new(true);

    public static FieldOperationStatus Failure(
        string error,
        FieldOperationFailureKind failureKind = FieldOperationFailureKind.DependencyUnavailable) =>
        new(false, error, failureKind);
}

public sealed record AxRecordKey(string? KeyField, string? KeyValue, long? RecordId);

public sealed record AxCreatePermission(bool IsAllowed, bool IsReadOnly = false);

public sealed record AxPermissionField(string Name, bool IsReadOnly);

public sealed record AxPermissionDataContainer(
    string Name,
    IReadOnlyList<AxPermissionField> Fields);

public static class AxPermissionSchema
{
    public static IReadOnlyList<AxPermissionDataContainer> FromState(AxDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.DataContainers.Values
            .Select(container => new AxPermissionDataContainer(
                container.Name,
                container.Fields
                    .Select(field => new AxPermissionField(field.Name, field.IsReadOnly))
                    .ToArray()))
            .ToArray();
    }
}

public sealed record AxQueryPermission(
    bool IsAllowed,
    bool CanEditTransaction,
    IReadOnlySet<string> IncludedDataContainers,
    IReadOnlySet<string> ExcludedDataContainers,
    IReadOnlyDictionary<string, IReadOnlySet<string>> IncludedFields,
    IReadOnlyDictionary<string, IReadOnlySet<string>> ExcludedFields,
    IReadOnlySet<string> ReadOnlyDataContainers,
    IReadOnlyDictionary<string, IReadOnlySet<string>> ReadOnlyFields,
    IReadOnlySet<string> AllowedOperations)
{
    public static AxQueryPermission Denied { get; } = new(
        false,
        false,
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}

public sealed record AxDataLoadResult(bool Succeeded, AxDataState? State, string? Error);

public sealed record AxpertInitializedData(
    string TransId,
    long? RecordId,
    IReadOnlyList<AxDataContainer> DataContainers,
    IReadOnlyList<AxFileReference> Files,
    bool AxOpen)
{
    public static AxpertInitializedData FromState(AxDataState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new(
            state.TransId,
            state.RecordId,
            state.DataContainers.Values.ToArray(),
            state.Files.ToArray(),
            state.AxOpen);
    }
}

public sealed record AxpertInitializationResult(
    bool Succeeded,
    AxpertInitializedData? Data,
    AxpertInitializationFailureKind FailureKind = AxpertInitializationFailureKind.DependencyUnavailable,
    string? Error = null)
{
    public static AxpertInitializationResult Success(AxpertInitializedData data) => new(true, data);

    public static AxpertInitializationResult Failure(
        AxpertInitializationFailureKind failureKind,
        string? error = null) => new(false, null, failureKind, error);
}

public enum AxpertInitializationFailureKind
{
    UnknownTransaction,
    DependencyUnavailable,
    ValidationFailed
}

public sealed record FillGridRow(IReadOnlyDictionary<string, object?> Values);

public sealed record FillGridResult(
    bool Succeeded,
    string TargetContainer,
    FillGridMode Mode,
    IReadOnlyList<FillGridRow> Rows,
    string? Error = null);

public sealed record FillGridExecutionResult(
    bool Succeeded,
    IReadOnlyList<FillGridResult> Operations,
    string? Error = null);

public sealed record AxSaveContext(string IdempotencyKey, AxUserContext User);

public sealed record AxSaveResult(bool Succeeded, long? RecordId, string? Error);
