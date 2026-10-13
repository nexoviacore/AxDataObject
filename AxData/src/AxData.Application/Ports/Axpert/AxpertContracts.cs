using AxData.Domain.Model;
using AxData.Application.Ports;

namespace AxData.Application.Ports.Axpert;

public sealed record StructDefHandle(
    string TenantId,
    string TransId,
    string Version,
    IReadOnlyList<DataContainerDefinition> DataContainers,
    IReadOnlyList<FieldDependency> Dependencies,
    IReadOnlyList<FillGridDefinition> FillGrids,
    StructDefRevisionInfo? RevisionInfo = null);

public sealed record StructDefRevisionInfo(
    string ContentHash,
    string? UpdatedOn,
    string? LegacyVersionNo,
    bool RunTimeStruct,
    bool RunTimeMode);

public sealed record DataContainerDefinition(
    string Name,
    bool IsGrid,
    IReadOnlyList<FieldDefinition> Fields,
    IReadOnlyList<string>? EvaluationOrder = null);

public sealed record FieldDefinition(string Name, bool IsReadOnly = false);

public sealed record FieldDependency(
    string SourceContainer,
    string SourceField,
    string TargetContainer,
    string TargetField,
    string ScriptName);

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

public sealed record AxRecordKey(string? KeyField, string? KeyValue, long? RecordId);

public sealed record AxCreatePermission(bool IsAllowed, bool IsReadOnly = false);

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
