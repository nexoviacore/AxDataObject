using System.Diagnostics;
using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Application.Set;

public sealed record SetFieldValue(string Name, object? Value);

public sealed record SetRowValue(
    string? RowId,
    string Action,
    IReadOnlyList<SetFieldValue> Fields);

public sealed record SetDataContainerValue(string Name, IReadOnlyList<SetRowValue> Rows);

public sealed record SetAxDataCommand(
    string ObjectId,
    bool GetDeps,
    IReadOnlyList<SetDataContainerValue> DataContainers,
    string? CorrelationId = null);

public sealed record SetAxDataResponse(
    string ObjectId,
    IReadOnlyList<SetCreatedRow> CreatedRows,
    IReadOnlyList<FieldChange> Changed,
    IReadOnlyList<FieldEvaluationError> Errors);

public sealed record SetCreatedRow(string DcId, int RequestRowIndex, string RowId);

public sealed record SetAxDataResult(
    SetAxDataResponse? Response,
    AxDataErrorDefinition? Error)
{
    public bool Succeeded => Response is not null;

    public static SetAxDataResult Success(SetAxDataResponse response) => new(response, null);

    public static SetAxDataResult Failure(AxDataErrorDefinition error) => new(null, error);
}

public sealed class SetAxDataHandler(
    IUserContext userContext,
    IAxDataStore store,
    IAxDataLock dataLock,
    IAxpertAuthorizationProvider authorizationProvider,
    IAxpertFieldEngine fieldEngine,
    IRowIdGenerator rowIdGenerator,
    IOperationalEventSink? operationalLog = null)
{
    /// <summary>Validates, authorizes, evaluates, and atomically stores a mutation of one tenant-scoped AxData object.</summary>
    /// <returns>Created rows, requested dependent changes, and field errors; or a safe operation-level error.</returns>
    /// <remarks>The object is loaded before permission evaluation, but no mutation runs without authorization and a token-owned lease.</remarks>
    public async Task<SetAxDataResult> HandleAsync(
        SetAxDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsValid(command))
        {
            return SetAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var actor = await userContext.GetCurrentAsync(cancellationToken);
        var redisStarted = Stopwatch.GetTimestamp();
        var state = await store.LoadAsync(actor.TenantId, command.ObjectId, cancellationToken);
        Log.Write(new OperationalEvent(
            "redis.load.completed", "set", state is null ? "miss" : "hit",
            Detail: OperationalLogDetail.Diagnostic, CorrelationId: command.CorrelationId,
            TenantId: actor.TenantId, ObjectId: command.ObjectId,
            DurationMs: Stopwatch.GetElapsedTime(redisStarted).TotalMilliseconds));
        if (state is null)
        {
            return SetAxDataResult.Failure(AxDataErrors.ObjectNotFound);
        }

        if (!string.Equals(state.TenantId, actor.TenantId, StringComparison.Ordinal) ||
            !string.Equals(state.ObjectId, command.ObjectId, StringComparison.Ordinal))
        {
            return SetAxDataResult.Failure(AxDataErrors.ObjectNotFound);
        }

        if (state.Status == AxDataLifecycleStatus.Expired)
        {
            return SetAxDataResult.Failure(AxDataErrors.ObjectExpired);
        }

        if (state.Status == AxDataLifecycleStatus.Closed)
        {
            return SetAxDataResult.Failure(AxDataErrors.ObjectNotFound);
        }

        if (state.Status != AxDataLifecycleStatus.Active)
        {
            return SetAxDataResult.Failure(AxDataErrors.ActiveConflict);
        }

        var authorizationStarted = Stopwatch.GetTimestamp();
        var permission = await authorizationProvider.GetSetPermissionAsync(
            actor.TenantId,
            state.TransId,
            new AxRecordKey(null, null, state.RecordId),
            actor,
            cancellationToken);
        var isAuthorized = IsAuthorized(actor, state, permission);
        Log.Write(new OperationalEvent(
            "authorization.completed", "set", isAuthorized ? "allowed" : "denied",
            isAuthorized ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId,
            ReasonCode: isAuthorized ? "policy-allowed" : "policy-denied",
            DurationMs: Stopwatch.GetElapsedTime(authorizationStarted).TotalMilliseconds));
        if (!isAuthorized)
        {
            return SetAxDataResult.Failure(AxDataErrors.AccessDenied);
        }

        var lockStarted = Stopwatch.GetTimestamp();
        await using var lease = await dataLock.TryAcquireAsync(
            actor.TenantId,
            command.ObjectId,
            cancellationToken);
        Log.Write(new OperationalEvent(
            "lock.acquire.completed", "set", lease is null ? "conflict" : "acquired",
            lease is null ? OperationalLogLevel.Warning : OperationalLogLevel.Information,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId,
            DurationMs: Stopwatch.GetElapsedTime(lockStarted).TotalMilliseconds));
        if (lease is null)
        {
            return SetAxDataResult.Failure(AxDataErrors.ActiveConflict);
        }

        state = CloneState(state);
        var started = await fieldEngine.CreateOperationAsync(state, actor, cancellationToken);
        if (!started.Succeeded || started.Operation is null || started.Operation.Definition is null)
        {
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        await using var operation = started.Operation;
        var definition = operation.Definition;
        Log.Write(new OperationalEvent(
            "structdef.binding.bypassed", "set", "current-definition",
            Detail: OperationalLogDetail.Diagnostic,
            CorrelationId: command.CorrelationId ?? Activity.Current?.TraceId.ToString(),
            TenantId: actor.TenantId, TransId: state.TransId, ObjectId: state.ObjectId));

        var preparation = PrepareRequest(
            command,
            state,
            definition,
            permission,
            rowIdGenerator);
        if (preparation.Error is not null)
        {
            return SetAxDataResult.Failure(preparation.Error);
        }

        var changes = new List<FieldChange>();
        var errors = new List<FieldEvaluationError>();
        var createdRows = new List<SetCreatedRow>();
        var changedParents = new List<FieldAddress>();
        var hasMutations = preparation.HasStructuralMutation;
        var fieldEvaluationStarted = Stopwatch.GetTimestamp();
        var preparedRows = preparation.Rows;

        var submissions = preparedRows
            .SelectMany(prepared => prepared.Fields.Select(field => new PreparedField(
                prepared,
                new FieldAddress(prepared.Container.Name, prepared.Row.RowId, field.Name),
                field.Value)))
            .ToArray();
        if (submissions.Length > 0)
        {
            var rejectedSources = new List<FieldAddress>();
            for (var submissionIndex = 0; submissionIndex < submissions.Length; submissionIndex++)
            {
                var submission = submissions[submissionIndex];
                if (IsBlockedByRejectedSource(submission.Address, rejectedSources, definition, state))
                {
                    continue;
                }

                var evaluation = await operation.EvaluateAsync(
                    submission.Address,
                    submission.Value,
                    cancellationToken);
                if (!evaluation.Succeeded)
                {
                    errors.AddRange(evaluation.Errors.Count == 0
                        ? [new FieldEvaluationError(submission.Address, AxDataErrors.ValidationFailed.Code, AxDataErrors.ValidationFailed.Message)]
                        : evaluation.Errors);
                    rejectedSources.Add(submission.Address);
                    var hasEligibleContinuation = submissions
                        .Skip(submissionIndex + 1)
                        .Any(candidate => !IsBlockedByRejectedSource(
                            candidate.Address,
                            rejectedSources,
                            definition,
                            state));
                    if (hasEligibleContinuation)
                    {
                        var rebuilt = await operation.RebuildAsync(cancellationToken);
                        if (!rebuilt.Succeeded)
                        {
                            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
                        }
                    }

                    continue;
                }

                submission.PreparedRow.HasValidField = true;
            }
        }

        foreach (var prepared in preparedRows)
        {
            if (prepared.IsNew && !prepared.HasValidField)
            {
                prepared.Container.Rows.Remove(prepared.Row);
                continue;
            }

            if (prepared.IsNew)
            {
                createdRows.Add(new SetCreatedRow(
                    prepared.Container.Name,
                    prepared.RequestRowIndex,
                    prepared.Row.RowId));
            }

            if (prepared.Submitted.Action == AxRowAction.Edit &&
                prepared.HasValidField &&
                prepared.Row.State != AxRowState.New)
            {
                prepared.Row.TransitionTo(AxRowState.Edited);
            }

            hasMutations |= prepared.HasValidField;
        }

        var completed = await operation.CompleteAsync(cancellationToken);
        if (!completed.Succeeded)
        {
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        changedParents.AddRange(completed.Changes.Select(change => change.Address));
        if (command.GetDeps)
        {
            changes.AddRange(completed.Changes.Where(change =>
                change.Source != FieldChangeSource.Submitted));
        }

        var fieldEvaluationDuration = Stopwatch.GetElapsedTime(fieldEvaluationStarted).TotalMilliseconds;
        Log.Write(new OperationalEvent(
            "validation.completed", "set", errors.Count == 0 ? "succeeded" : "validation-errors",
            errors.Count == 0 ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId, ErrorCode: errors.Count == 0 ? null : AxDataErrors.ValidationFailed.Code,
            Count: errors.Count, DurationMs: fieldEvaluationDuration));
        Log.Write(new OperationalEvent(
            "dependency.evaluation.completed", "set", "completed",
            Detail: OperationalLogDetail.Diagnostic, CorrelationId: command.CorrelationId,
            TenantId: actor.TenantId, TransId: state.TransId, ObjectId: state.ObjectId,
            Count: changedParents.Count, DurationMs: fieldEvaluationDuration));

        if (!hasMutations)
        {
            return SetAxDataResult.Success(new SetAxDataResponse(state.ObjectId, createdRows, changes, errors));
        }

        var fillGridStarted = Stopwatch.GetTimestamp();
        var fillGrid = await operation.ExecuteApplicableFillGridAsync(
            state,
            changedParents,
            actor,
            cancellationToken);
        if (!fillGrid.Succeeded)
        {
            LogFillGrid("failed", OperationalLogLevel.Warning, 0, 0);
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        if (!TryBuildFillGridPlan(
                state,
                definition,
                fillGrid.Operations,
                rowIdGenerator,
                out var fillGridPlan))
        {
            LogFillGrid("invalid-result", OperationalLogLevel.Warning, fillGrid.Operations.Count, 0);
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        var fillGridChanges = new List<FieldChange>();
        foreach (var planned in fillGridPlan)
        {
            if (planned.Mode == FillGridMode.Replace)
            {
                planned.Container.Rows.Clear();
            }

            foreach (var row in planned.Rows)
            {
                planned.Container.AddRow(row);
            }

            fillGridChanges.AddRange(planned.Changes);
        }

        if (command.GetDeps)
        {
            changes.AddRange(fillGridChanges);
        }

        LogFillGrid("succeeded", OperationalLogLevel.Information, fillGrid.Operations.Count, fillGridChanges.Count);

        state.RegisterMutation();
        var writeStarted = Stopwatch.GetTimestamp();
        var write = await store.WriteAsync(
            state,
            lease.Token,
            cancellationToken);
        Log.Write(new OperationalEvent(
            "redis.write.completed", "set",
            write.Status == AxDataStoreWriteStatus.Stored ? "stored" : "ownership-lost",
            write.Status == AxDataStoreWriteStatus.Stored ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId,
            DurationMs: Stopwatch.GetElapsedTime(writeStarted).TotalMilliseconds));
        if (write.Status == AxDataStoreWriteStatus.Conflict)
        {
            return SetAxDataResult.Failure(AxDataErrors.ActiveConflict);
        }

        return SetAxDataResult.Success(new SetAxDataResponse(state.ObjectId, createdRows, changes, errors));

        void LogFillGrid(string outcome, OperationalLogLevel level, int operationCount, int changeCount) =>
            Log.Write(new OperationalEvent(
                "fillgrid.completed", "set", outcome, level, OperationalLogDetail.Diagnostic,
                command.CorrelationId, actor.TenantId, state.TransId, state.ObjectId,
                ErrorCode: level == OperationalLogLevel.Warning ? AxDataErrors.DependencyUnavailable.Code : null,
                Count: operationCount, SecondaryCount: changeCount,
                DurationMs: Stopwatch.GetElapsedTime(fillGridStarted).TotalMilliseconds));
    }

    private IOperationalEventSink Log => operationalLog ?? NullOperationalEventSink.Instance;

    private static bool IsAuthorized(AxUserContext actor, AxDataState state, AxQueryPermission permission)
    {
        var context = AxAuthorizationContext.FromState(
            actor,
            state,
            hasViewerAccess: permission.IsAllowed,
            hasOperationPermission: permission.IsAllowed && permission.AllowedOperations.Contains("set"),
            isEditable: permission.CanEditTransaction);
        return AxAuthorizationPolicy.IsAllowed(context, AxAuthorizationOperation.Set);
    }

    /// <summary>Validates requested containers, rows, fields, and permissions while preparing an isolated state mutation.</summary>
    /// <returns>Prepared rows and structural-mutation state, or the first request-level error.</returns>
    private static PreparedRequest PrepareRequest(
        SetAxDataCommand command,
        AxDataState state,
        StructDefHandle definition,
        AxQueryPermission permission,
        IRowIdGenerator rowIdGenerator)
    {
        var definitions = definition.DataContainers.ToDictionary(
            container => container.Name,
            StringComparer.OrdinalIgnoreCase);
        var containers = state.DataContainers.Values.ToDictionary(
            container => container.Name,
            StringComparer.OrdinalIgnoreCase);
        var definitionFields = definitions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var container in containers.Values)
        {
            if (!definitions.TryGetValue(container.Name, out var containerDefinition) ||
                container.Fields.Count != containerDefinition.Fields.Count)
            {
                return PreparedRequest.Failure(AxDataErrors.RecoveryFailed);
            }

            var currentFields = definitionFields[container.Name];
            if (container.Fields.Any(field =>
                    !currentFields.TryGetValue(field.Name, out var current) ||
                    field.IsSourceKey != current.SourceKey ||
                    field.IsReadOnly != current.IsReadOnly))
            {
                return PreparedRequest.Failure(AxDataErrors.RecoveryFailed);
            }
        }

        var plans = new List<PreparedRowPlan>();
        foreach (var submittedContainer in command.DataContainers)
        {
            if (!definitions.TryGetValue(submittedContainer.Name, out var containerDefinition) ||
                !containers.TryGetValue(submittedContainer.Name, out var container))
            {
                return PreparedRequest.Failure(AxDataErrors.InvalidRequest);
            }

            if (!CanWriteContainer(submittedContainer.Name, permission))
            {
                return PreparedRequest.Failure(AxDataErrors.AccessDenied);
            }

            var rows = container.Rows.ToDictionary(row => row.RowId, StringComparer.OrdinalIgnoreCase);
            var fields = definitionFields[container.Name];
            var declaredOrder = containerDefinition.EvaluationOrder ?? [];
            var evaluationOrder = declaredOrder
                .Concat(containerDefinition.Fields
                    .Select(field => field.Name)
                    .Where(name => !declaredOrder.Contains(name, StringComparer.OrdinalIgnoreCase)))
                .Select((name, index) => (name, index))
                .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);
            for (var rowIndex = 0; rowIndex < submittedContainer.Rows.Count; rowIndex++)
            {
                var submittedRow = submittedContainer.Rows[rowIndex];
                var existing = !string.IsNullOrWhiteSpace(submittedRow.RowId) &&
                               rows.TryGetValue(submittedRow.RowId, out var found)
                    ? found
                    : null;
                if (submittedRow.Action == AxRowAction.New)
                {
                    if (!string.IsNullOrWhiteSpace(submittedRow.RowId) || existing is not null ||
                        !container.IsGrid && container.Rows.Count != 0)
                    {
                        return PreparedRequest.Failure(AxDataErrors.InvalidRequest);
                    }
                }
                else if (existing is null || existing.State == AxRowState.Deleted)
                {
                    return PreparedRequest.Failure(AxDataErrors.InvalidRequest);
                }

                if (submittedRow.Action == AxRowAction.Delete && submittedRow.Fields.Count != 0)
                {
                    return PreparedRequest.Failure(AxDataErrors.InvalidRequest);
                }

                foreach (var submittedField in submittedRow.Fields)
                {
                    if (!fields.TryGetValue(submittedField.Name, out var fieldDefinition))
                    {
                        return PreparedRequest.Failure(AxDataErrors.InvalidRequest);
                    }

                    if (fieldDefinition.IsReadOnly ||
                        existing?.Fields.TryGetValue(submittedField.Name, out var field) == true && field.IsReadOnly ||
                        IsReadOnlyField(submittedContainer.Name, submittedField.Name, permission) ||
                        !IsIncludedField(submittedContainer.Name, submittedField.Name, permission))
                    {
                        return PreparedRequest.Failure(AxDataErrors.AccessDenied);
                    }
                }

                plans.Add(new PreparedRowPlan(
                    container,
                    containerDefinition,
                    submittedRow,
                    rowIndex,
                    existing,
                    submittedRow.Fields
                        .OrderBy(field => evaluationOrder[field.Name])
                        .ToArray()));
            }
        }

        var prepared = new List<PreparedRow>();
        var hasStructuralMutation = false;
        foreach (var plan in plans)
        {
            var row = plan.Existing;
            if (plan.Submitted.Action == AxRowAction.New)
            {
                var rowFields = plan.Definition.Fields.ToDictionary(
                    field => field.Name,
                    field => new AxFieldValue(null, field.IsReadOnly, isSourceKey: field.SourceKey),
                    StringComparer.OrdinalIgnoreCase);
                row = new AxDataRow(
                    NewUniqueRowId(plan.Container, [], rowIdGenerator),
                    AxRowState.New,
                    fields: rowFields);
                plan.Container.AddRow(row);
                hasStructuralMutation = true;
            }

            if (plan.Submitted.Action == AxRowAction.Delete)
            {
                plan.Container.ApplyRowState(row!.RowId, AxRowState.Deleted);
                hasStructuralMutation = true;
                continue;
            }

            prepared.Add(new PreparedRow(
                plan.Container,
                row!,
                plan.Submitted,
                plan.RequestRowIndex,
                plan.Submitted.Action == AxRowAction.New,
                plan.OrderedFields));
        }

        return new PreparedRequest(prepared, hasStructuralMutation, null);
    }

    /// <summary>Determines whether a field transitively depends on any source rejected earlier in this operation.</summary>
    private static bool IsBlockedByRejectedSource(
        FieldAddress candidate,
        IReadOnlyCollection<FieldAddress> rejectedSources,
        StructDefHandle definition,
        AxDataState state) =>
        rejectedSources.Any(rejected => IsReachable(rejected, candidate, definition, state));

    /// <summary>Traverses the field-dependency graph while mapping parent and grid-row addresses.</summary>
    private static bool IsReachable(
        FieldAddress source,
        FieldAddress candidate,
        StructDefHandle definition,
        AxDataState state)
    {
        var pending = new Queue<FieldAddress>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        pending.Enqueue(source);
        visited.Add(AddressKey(source));

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var dependency in definition.Dependencies
                         .Where(dependency =>
                             dependency.IsResolved &&
                             dependency.TargetContainer is not null &&
                             IsOrdinaryDependency(dependency.Kind) &&
                             string.Equals(
                                 dependency.SourceContainer,
                                 current.DataContainer,
                                 StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(
                                 dependency.SourceField,
                                 current.FieldName,
                                 StringComparison.OrdinalIgnoreCase))
                         .OrderBy(dependency => dependency.DeclarationOrder))
            {
                if (!TryMapDependentAddress(state, current, dependency, out var target))
                {
                    continue;
                }

                if (AddressesEqual(target, candidate))
                {
                    return true;
                }

                if (visited.Add(AddressKey(target)))
                {
                    pending.Enqueue(target);
                }
            }
        }

        return false;
    }

    private static bool TryMapDependentAddress(
        AxDataState state,
        FieldAddress source,
        FieldDependency dependency,
        out FieldAddress target)
    {
        target = null!;
        if (!state.DataContainers.TryGetValue(source.DataContainer, out var sourceContainer) ||
            dependency.TargetContainer is null ||
            !state.DataContainers.TryGetValue(dependency.TargetContainer, out var targetContainer))
        {
            return false;
        }

        var sourceRowIndex = sourceContainer.Rows.FindIndex(row =>
            string.Equals(row.RowId, source.RowId, StringComparison.OrdinalIgnoreCase));
        if (sourceRowIndex < 0)
        {
            return false;
        }

        var sourceRowNumber = sourceContainer.IsGrid ? sourceRowIndex + 1 : 1;
        var targetRowIndex = dependency.TargetIsGrid ? sourceRowNumber - 1 : 0;
        if (targetRowIndex < 0 || targetRowIndex >= targetContainer.Rows.Count ||
            !targetContainer.Rows[targetRowIndex].Fields.ContainsKey(dependency.TargetField))
        {
            return false;
        }

        target = new FieldAddress(
            targetContainer.Name,
            targetContainer.Rows[targetRowIndex].RowId,
            dependency.TargetField);
        return true;
    }

    private static bool IsOrdinaryDependency(FieldDependencyKind kind) => kind is
        FieldDependencyKind.Expression or
        FieldDependencyKind.Select or
        FieldDependencyKind.Fill or
        FieldDependencyKind.Aggregate;

    private static bool AddressesEqual(FieldAddress left, FieldAddress right) =>
        string.Equals(left.DataContainer, right.DataContainer, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.RowId, right.RowId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.FieldName, right.FieldName, StringComparison.OrdinalIgnoreCase);

    private static string AddressKey(FieldAddress address) =>
        $"{address.DataContainer}\n{address.RowId}\n{address.FieldName}";

    /// <summary>Builds a permission-filtered FillGrid response plan without exposing hidden containers or fields.</summary>
    private static bool TryBuildFillGridPlan(
        AxDataState state,
        StructDefHandle definition,
        IReadOnlyList<FillGridResult> operations,
        IRowIdGenerator rowIdGenerator,
        out IReadOnlyList<PreparedFillGridOperation> plan)
    {
        var prepared = new List<PreparedFillGridOperation>(operations.Count);
        var pendingRows = new Dictionary<string, List<AxDataRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var operation in operations)
        {
            if (!operation.Succeeded || !CanApplyFillGrid(state, definition, operation))
            {
                plan = [];
                return false;
            }

            var containerDefinition = definition.DataContainers.Single(candidate =>
                string.Equals(candidate.Name, operation.TargetContainer, StringComparison.OrdinalIgnoreCase));
            var container = state.DataContainers[containerDefinition.Name];
            if (!pendingRows.TryGetValue(container.Name, out var allPending))
            {
                allPending = [];
                pendingRows.Add(container.Name, allPending);
            }

            var generatedRows = new List<AxDataRow>(operation.Rows.Count);
            var generatedChanges = new List<FieldChange>();
            foreach (var fillGridRow in operation.Rows)
            {
                var id = NewUniqueRowId(container, allPending, rowIdGenerator);
                var fields = containerDefinition.Fields.ToDictionary(
                    field => field.Name,
                    field => new AxFieldValue(
                        fillGridRow.Values.TryGetValue(field.Name, out var value) ? value : null,
                        field.IsReadOnly,
                        isSourceKey: field.SourceKey),
                    StringComparer.OrdinalIgnoreCase);
                var row = new AxDataRow(id, AxRowState.New, fields: fields);
                generatedRows.Add(row);
                allPending.Add(row);
                generatedChanges.AddRange(fillGridRow.Values.Select(value => new FieldChange(
                    new FieldAddress(container.Name, id, value.Key),
                    value.Value,
                    FieldChangeSource.FillGrid)));
            }

            prepared.Add(new PreparedFillGridOperation(
                container,
                operation.Mode,
                generatedRows,
                generatedChanges));
        }

        plan = prepared;
        return true;
    }

    private static bool CanApplyFillGrid(
        AxDataState state,
        StructDefHandle definition,
        FillGridResult fillGrid)
    {
        var containerDefinition = definition.DataContainers.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, fillGrid.TargetContainer, StringComparison.OrdinalIgnoreCase));
        if (fillGrid.Mode is not (FillGridMode.Append or FillGridMode.Replace) ||
            containerDefinition is null || !containerDefinition.IsGrid ||
            !state.DataContainers.TryGetValue(containerDefinition.Name, out var container) || !container.IsGrid)
        {
            return false;
        }

        var knownFields = containerDefinition.Fields
            .Select(field => field.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return fillGrid.Rows.All(row =>
            row.Values.Keys.All(knownFields.Contains) &&
            row.Values.Values.All(IsSupportedFieldValue));
    }

    private static bool IsSupportedFieldValue(object? value) => value switch
    {
        float typed => float.IsFinite(typed),
        double typed => double.IsFinite(typed),
        null or string or bool or byte or short or int or long or decimal or
            DateTime or DateTimeOffset or DateOnly or TimeOnly or Guid or byte[] => true,
        _ => false
    };

    private static bool IsValid(SetAxDataCommand command) =>
        !string.IsNullOrWhiteSpace(command.ObjectId) &&
        command.DataContainers is { Count: > 0 } &&
        command.DataContainers.All(container =>
            !string.IsNullOrWhiteSpace(container.Name) &&
            container.Rows is { Count: > 0 } &&
            container.Rows.All(row =>
                row.Action is AxRowAction.New or AxRowAction.Edit or AxRowAction.Delete &&
                (row.Action == AxRowAction.New
                    ? string.IsNullOrWhiteSpace(row.RowId)
                    : !string.IsNullOrWhiteSpace(row.RowId)) &&
                row.Fields is not null &&
                (row.Action == AxRowAction.Delete || row.Fields.Count > 0) &&
                row.Fields.All(field => !string.IsNullOrWhiteSpace(field.Name)) &&
                row.Fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == row.Fields.Count) &&
            container.Rows.Where(row => row.Action != AxRowAction.New)
                .Select(row => row.RowId).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
            container.Rows.Count(row => row.Action != AxRowAction.New)) &&
        command.DataContainers.Select(container => container.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == command.DataContainers.Count;

    /// <summary>Creates the isolated working copy mutated under the object lease.</summary>
    private static AxDataState CloneState(AxDataState source)
    {
        var containers = source.DataContainers.Values.Select(container =>
            new AxDataContainer(
                container.Name,
                container.IsGrid,
                container.Rows.Select(row => new AxDataRow(
                    row.RowId,
                    row.State,
                    row.PersistedRecordId,
                    row.Fields.ToDictionary(
                        pair => pair.Key,
                        pair => AxFieldValue.Rehydrate(
                            pair.Value.Value,
                            pair.Value.OriginalValue,
                            pair.Value.IsDirty,
                            pair.Value.IsReadOnly,
                            pair.Value.SourceId,
                            pair.Value.OriginalSourceId,
                            pair.Value.IsSourceKey),
                        StringComparer.OrdinalIgnoreCase))),
                container.Fields.Select(field => new AxDataFieldMetadata(
                    field.Name,
                    field.IsReadOnly,
                    field.IsSourceKey))));
        var workflow = source.Workflow is null
            ? null
            : new PegActiveState
            {
                WorkflowInstanceId = source.Workflow.WorkflowInstanceId,
                CurrentTaskId = source.Workflow.CurrentTaskId,
                InitiatorUserName = source.Workflow.InitiatorUserName,
                Status = source.Workflow.Status,
                ApprovalLevel = source.Workflow.ApprovalLevel,
                Approvers = source.Workflow.Approvers.ToArray(),
                WorkflowVersion = source.Workflow.WorkflowVersion,
                PendingFromUtc = source.Workflow.PendingFromUtc
            };

        return AxDataState.Rehydrate(
            source.ObjectId,
            source.TenantId,
            source.TransId,
            source.RecordId,
            source.InitiatorUserName,
            source.CreatedAtUtc,
            source.LastAccessedAtUtc,
            source.Status,
            containers,
            workflow,
            source.Files.ToArray(),
            source.AxOpen);
    }

    private static string NewUniqueRowId(
        AxDataContainer container,
        IReadOnlyCollection<AxDataRow> pending,
        IRowIdGenerator rowIdGenerator)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var rowId = rowIdGenerator.NewRowId();
            if (!container.Rows.Any(row => string.Equals(row.RowId, rowId, StringComparison.OrdinalIgnoreCase)) &&
                !pending.Any(row => string.Equals(row.RowId, rowId, StringComparison.OrdinalIgnoreCase)))
            {
                return rowId;
            }
        }

        throw new InvalidOperationException("A unique row ID could not be generated.");
    }

    private static bool CanWriteContainer(string name, AxQueryPermission permission) =>
        !permission.ReadOnlyDataContainers.Contains(name) &&
        !permission.ExcludedDataContainers.Contains(name) &&
        (permission.IncludedDataContainers.Count == 0 || permission.IncludedDataContainers.Contains("*") ||
         permission.IncludedDataContainers.Contains(name));

    private static bool IsIncludedField(string container, string field, AxQueryPermission permission)
    {
        var included = permission.IncludedFields.TryGetValue(container, out var includes) ? includes : null;
        var excluded = permission.ExcludedFields.TryGetValue(container, out var excludes) ? excludes : null;
        return (excluded is null || !excluded.Contains(field)) &&
               (included is null || included.Count == 0 || included.Contains("*") || included.Contains(field));
    }

    private static bool IsReadOnlyField(string container, string field, AxQueryPermission permission) =>
        permission.ReadOnlyFields.TryGetValue(container, out var fields) && fields.Contains(field);

    private sealed class PreparedRow(
        AxDataContainer container,
        AxDataRow row,
        SetRowValue submitted,
        int requestRowIndex,
        bool isNew,
        IReadOnlyList<SetFieldValue> fields)
    {
        public AxDataContainer Container { get; } = container;
        public AxDataRow Row { get; } = row;
        public SetRowValue Submitted { get; } = submitted;
        public int RequestRowIndex { get; } = requestRowIndex;
        public bool IsNew { get; } = isNew;
        public IReadOnlyList<SetFieldValue> Fields { get; } = fields;
        public bool HasValidField { get; set; }
    }

    private sealed record PreparedRowPlan(
        AxDataContainer Container,
        DataContainerDefinition Definition,
        SetRowValue Submitted,
        int RequestRowIndex,
        AxDataRow? Existing,
        IReadOnlyList<SetFieldValue> OrderedFields);

    private sealed record PreparedRequest(
        IReadOnlyList<PreparedRow> Rows,
        bool HasStructuralMutation,
        AxDataErrorDefinition? Error)
    {
        public static PreparedRequest Failure(AxDataErrorDefinition error) =>
            new([], false, error);
    }

    private sealed record PreparedFillGridOperation(
        AxDataContainer Container,
        FillGridMode Mode,
        IReadOnlyList<AxDataRow> Rows,
        IReadOnlyList<FieldChange> Changes);

    private sealed record PreparedField(PreparedRow PreparedRow, FieldAddress Address, object? Value);
}
