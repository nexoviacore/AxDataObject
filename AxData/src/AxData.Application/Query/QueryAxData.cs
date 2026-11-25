using System.Diagnostics;
using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Application.Query;

public sealed record QuerySelection(string DataContainer, IReadOnlyList<string> Fields);

public sealed record QueryAxDataCommand(
    string ObjectId,
    bool SelectAll,
    IReadOnlyList<QuerySelection> Selections);

public sealed record QueryFieldResponse(string Name, object? Value, bool ReadOnly);

public sealed record QueryRowResponse(
    string RowId,
    long? RecordId,
    string? Action,
    IReadOnlyList<QueryFieldResponse> Fields);

public sealed record QueryDataContainerResponse(
    string DcId,
    bool IsGrid,
    bool ReadOnly,
    IReadOnlyList<QueryRowResponse> Rows);

public sealed record QueryAxDataResponse(
    string ObjectId,
    string TransId,
    long? RecordId,
    bool ReadOnly,
    bool PendingApproval,
    IReadOnlyList<string> AllowedOperations,
    IReadOnlyList<QueryDataContainerResponse> DataContainers);

public sealed record QueryAxDataResult(QueryAxDataResponse? Response, AxDataErrorDefinition? Error)
{
    public bool Succeeded => Response is not null;

    public static QueryAxDataResult Success(QueryAxDataResponse response) => new(response, null);

    public static QueryAxDataResult Failure(AxDataErrorDefinition error) => new(null, error);
}

public sealed class QueryAxDataHandler(
    IUserContext userContext,
    IAxDataStore store,
    IAxpertAuthorizationProvider authorizationProvider,
    IOperationalEventSink? operationalLog = null)
{
    /// <summary>Loads, authorizes, and projects one tenant-scoped AxData object for the current user.</summary>
    /// <returns>A permission-filtered projection, or a safe application error.</returns>
    /// <remarks>Hidden containers and fields are omitted. A mismatched tenant or object identity is treated as not found.</remarks>
    public async Task<QueryAxDataResult> HandleAsync(
        QueryAxDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsValid(command))
        {
            return QueryAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var actor = await userContext.GetCurrentAsync(cancellationToken);
        var redisStarted = Stopwatch.GetTimestamp();
        var state = await store.LoadAsync(actor.TenantId, command.ObjectId, cancellationToken);
        Log.Write(new OperationalEvent(
            "redis.load.completed", "query", state is null ? "miss" : "hit",
            Detail: OperationalLogDetail.Diagnostic,
            TenantId: actor.TenantId, ObjectId: command.ObjectId,
            DurationMs: Stopwatch.GetElapsedTime(redisStarted).TotalMilliseconds));
        if (state is null)
        {
            return QueryAxDataResult.Failure(AxDataErrors.ObjectNotFound);
        }

        if (state.Status == AxDataLifecycleStatus.Expired)
        {
            return QueryAxDataResult.Failure(AxDataErrors.ObjectExpired);
        }

        if (state.Status == AxDataLifecycleStatus.Closed)
        {
            return QueryAxDataResult.Failure(AxDataErrors.ObjectNotFound);
        }

        var authorizationStarted = Stopwatch.GetTimestamp();
        var permission = await authorizationProvider.GetQueryPermissionAsync(
            actor.TenantId,
            state.TransId,
            new AxRecordKey(null, null, state.RecordId),
            actor,
            AxPermissionSchema.FromState(state),
            cancellationToken);

        var authorization = AxAuthorizationContext.FromState(
            actor,
            state,
            hasViewerAccess: permission.IsAllowed,
            hasOperationPermission: permission.IsAllowed,
            isEditable: permission.CanEditTransaction);
        var isAuthorized = AxAuthorizationPolicy.IsAllowed(authorization, AxAuthorizationOperation.Query);
        Log.Write(new OperationalEvent(
            "authorization.completed", "query", isAuthorized ? "allowed" : "denied",
            isAuthorized ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            TenantId: actor.TenantId, TransId: state.TransId, ObjectId: state.ObjectId,
            ReasonCode: isAuthorized ? "policy-allowed" : "policy-denied",
            DurationMs: Stopwatch.GetElapsedTime(authorizationStarted).TotalMilliseconds));
        if (!isAuthorized)
        {
            return QueryAxDataResult.Failure(AxDataErrors.AccessDenied);
        }

        var projection = Project(command, state, permission, authorization);
        return projection is null
            ? QueryAxDataResult.Failure(AxDataErrors.InvalidRequest)
            : QueryAxDataResult.Success(projection);
    }

    private IOperationalEventSink Log => operationalLog ?? NullOperationalEventSink.Instance;

    /// <summary>Projects only requested and permission-visible state into the query contract.</summary>
    /// <returns>The filtered response, or <see langword="null"/> when the requested selection is invalid.</returns>
    private static QueryAxDataResponse? Project(
        QueryAxDataCommand command,
        AxDataState state,
        AxQueryPermission permission,
        AxAuthorizationContext authorization)
    {
        var requested = command.SelectAll
            ? null
            : command.Selections.ToDictionary(
                selection => selection.DataContainer,
                selection => selection.Fields,
                StringComparer.OrdinalIgnoreCase);

        if (requested is not null && requested.Keys.Any(name => !state.DataContainers.ContainsKey(name)))
        {
            return null;
        }

        var transactionReadOnly =
            state.Status != AxDataLifecycleStatus.Active ||
            !permission.CanEditTransaction ||
            !HasAnyRelation(authorization, AxActorRelation.Initiator);
        var containers = new List<QueryDataContainerResponse>();
        foreach (var container in state.DataContainers.Values)
        {
            IReadOnlyList<string>? requestedFields = null;
            if (requested is not null && !requested.TryGetValue(container.Name, out requestedFields))
            {
                continue;
            }

            if (!IsVisible(container.Name, permission.IncludedDataContainers, permission.ExcludedDataContainers))
            {
                continue;
            }

            var selectAllFields = requested is null || requestedFields!.Any(IsWildcard);
            var knownFields = container.Fields
                .Select(field => field.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!selectAllFields && requestedFields!.Any(field => !knownFields.Contains(field)))
            {
                return null;
            }

            var containerReadOnly = transactionReadOnly || permission.ReadOnlyDataContainers.Contains(container.Name);
            var rows = container.Rows.Select(row => new QueryRowResponse(
                row.RowId,
                row.PersistedRecordId,
                row.State == AxRowState.Unchanged ? null : AxRowAction.ToCode(row.State),
                container.Fields
                    .Where(field => selectAllFields || requestedFields!.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                    .Where(field => IsFieldVisible(container.Name, field.Name, permission))
                    .Select(field => new QueryFieldResponse(
                        field.Name,
                        row.Fields[field.Name].Value,
                        containerReadOnly ||
                        field.IsReadOnly ||
                        IsReadOnlyField(container.Name, field.Name, permission)))
                    .ToArray()))
                .ToArray();

            containers.Add(new QueryDataContainerResponse(
                container.Name,
                container.IsGrid,
                containerReadOnly,
                rows));
        }

        var pendingApproval = HasAnyRelation(authorization, AxActorRelation.CurrentApprover) &&
                              state.Workflow is { Status: PegWorkflowStatus.Pending, CurrentTaskId: not null };
        var allowedOperations = Enum.GetValues<AxAuthorizationOperation>()
            .Where(operation => operation != AxAuthorizationOperation.Initialize)
            .Where(operation => permission.AllowedOperations.Contains(ToContractName(operation)))
            .Where(operation => AxAuthorizationPolicy.IsAllowed(
                authorization with { HasOperationPermission = true },
                operation))
            .Where(operation => state.Status == AxDataLifecycleStatus.Active || operation == AxAuthorizationOperation.Query)
            .Select(ToContractName)
            .ToArray();

        return new QueryAxDataResponse(
            state.ObjectId,
            state.TransId,
            state.RecordId,
            transactionReadOnly,
            pendingApproval,
            allowedOperations,
            containers);
    }

    private static bool IsValid(QueryAxDataCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ObjectId) || command.Selections is null ||
            (command.SelectAll ? command.Selections.Count != 0 : command.Selections.Count == 0))
        {
            return false;
        }

        return command.Selections.All(selection =>
                   !string.IsNullOrWhiteSpace(selection.DataContainer) &&
                   selection.Fields is { Count: > 0 } &&
                   selection.Fields.All(field => !string.IsNullOrWhiteSpace(field))) &&
               command.Selections
                   .Select(selection => selection.DataContainer)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .Count() == command.Selections.Count;
    }

    private static bool IsVisible(string name, IReadOnlySet<string> included, IReadOnlySet<string> excluded) =>
        !excluded.Contains(name) &&
        (included.Count == 0 || included.Contains("*") || included.Contains(name));

    private static bool IsFieldVisible(string container, string field, AxQueryPermission permission)
    {
        var included = permission.IncludedFields.TryGetValue(container, out var includeSet) ? includeSet : null;
        var excluded = permission.ExcludedFields.TryGetValue(container, out var excludeSet) ? excludeSet : null;
        return (excluded is null || !excluded.Contains(field)) &&
               (included is null || included.Count == 0 || included.Contains("*") || included.Contains(field));
    }

    private static bool IsReadOnlyField(string container, string field, AxQueryPermission permission) =>
        permission.ReadOnlyFields.TryGetValue(container, out var fields) && fields.Contains(field);

    private static bool HasAnyRelation(AxAuthorizationContext context, AxActorRelation relations) =>
        (context.Relations & relations) != AxActorRelation.Unrelated;

    private static bool IsWildcard(string field) => string.Equals(field, "*", StringComparison.Ordinal);

    private static string ToContractName(AxAuthorizationOperation operation) =>
        operation.ToString().ToLowerInvariant();
}
