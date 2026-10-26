using System.Diagnostics;
using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Application.Recovery;
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
    long ExpectedVersion,
    bool GetDeps,
    IReadOnlyList<SetDataContainerValue> DataContainers,
    string? CorrelationId = null);

public sealed record SetAxDataResponse(
    string ObjectId,
    long Version,
    IReadOnlyList<SetCreatedRow> CreatedRows,
    IReadOnlyList<FieldChange> Changed,
    IReadOnlyList<FieldEvaluationError> Errors);

public sealed record SetCreatedRow(string DcId, int RequestRowIndex, string RowId);

public sealed record SetAxDataResult(
    SetAxDataResponse? Response,
    AxDataErrorDefinition? Error,
    long? CurrentVersion = null)
{
    public bool Succeeded => Response is not null;

    public static SetAxDataResult Success(SetAxDataResponse response) => new(response, null);

    public static SetAxDataResult Failure(AxDataErrorDefinition error, long? currentVersion = null) =>
        new(null, error, currentVersion);
}

public sealed class SetAxDataHandler(
    IUserContext userContext,
    IAxDataStore store,
    IAxDataLock dataLock,
    IStructDefProvider structDefProvider,
    IAxpertAuthorizationProvider authorizationProvider,
    IAxpertFieldEngine fieldEngine,
    IAxpertFillGridEngine fillGridEngine,
    ISnapshotTransport snapshotTransport,
    IRowIdGenerator rowIdGenerator,
    IClock clock,
    AxDataRecoveryService recoveryService,
    IOperationalEventSink? operationalLog = null)
{
    public async Task<SetAxDataResult> HandleAsync(
        SetAxDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsValid(command))
        {
            return SetAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var actor = await userContext.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var redisStarted = Stopwatch.GetTimestamp();
        var state = await store.LoadAsync(actor.TenantId, command.ObjectId, cancellationToken).ConfigureAwait(false);
        Log.Write(new OperationalEvent(
            "redis.load.completed", "set", state is null ? "miss" : "hit",
            Detail: OperationalLogDetail.Diagnostic, CorrelationId: command.CorrelationId,
            TenantId: actor.TenantId, ObjectId: command.ObjectId, Version: state?.Version,
            DurationMs: Stopwatch.GetElapsedTime(redisStarted).TotalMilliseconds));
        if (state is null)
        {
            var recovery = await recoveryService.RecoverAsync(
                actor,
                command.ObjectId,
                AxAuthorizationOperation.Set,
                cancellationToken).ConfigureAwait(false);
            if (!recovery.Succeeded)
            {
                return SetAxDataResult.Failure(recovery.Error!);
            }

            state = recovery.State!;
        }

        if (state.Status == AxDataLifecycleStatus.Expired)
        {
            return SetAxDataResult.Failure(AxDataErrors.ObjectExpired);
        }

        var authorizationStarted = Stopwatch.GetTimestamp();
        var permission = await authorizationProvider.GetSetPermissionAsync(
            actor.TenantId,
            state.TransId,
            new AxRecordKey(state.KeyField, state.KeyValue, state.RecordId),
            actor,
            cancellationToken).ConfigureAwait(false);
        var isAuthorized = IsAuthorized(actor, state, permission);
        Log.Write(new OperationalEvent(
            "authorization.completed", "set", isAuthorized ? "allowed" : "denied",
            isAuthorized ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId, Version: state.Version,
            ReasonCode: isAuthorized ? "policy-allowed" : "policy-denied",
            DurationMs: Stopwatch.GetElapsedTime(authorizationStarted).TotalMilliseconds));
        if (!isAuthorized)
        {
            return SetAxDataResult.Failure(AxDataErrors.AccessDenied);
        }

        var definition = await structDefProvider.GetAsync(
            actor.TenantId,
            state.TransId,
            cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        if (!MatchesDefinition(state, definition))
        {
            return SetAxDataResult.Failure(AxDataErrors.StructDefChanged);
        }

        var lockStarted = Stopwatch.GetTimestamp();
        await using var lease = await dataLock.TryAcquireAsync(
            actor.TenantId,
            command.ObjectId,
            cancellationToken).ConfigureAwait(false);
        Log.Write(new OperationalEvent(
            "lock.acquire.completed", "set", lease is null ? "conflict" : "acquired",
            lease is null ? OperationalLogLevel.Warning : OperationalLogLevel.Information,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId, Version: state.Version,
            DurationMs: Stopwatch.GetElapsedTime(lockStarted).TotalMilliseconds));
        if (lease is null)
        {
            return SetAxDataResult.Failure(AxDataErrors.ActiveConflict);
        }

        state = await store.LoadAsync(actor.TenantId, command.ObjectId, cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            var recovery = await recoveryService.RecoverAsync(
                actor,
                command.ObjectId,
                AxAuthorizationOperation.Set,
                cancellationToken).ConfigureAwait(false);
            if (!recovery.Succeeded)
            {
                return SetAxDataResult.Failure(recovery.Error!);
            }

            state = recovery.State!;
        }

        if (state.Version != command.ExpectedVersion)
        {
            return SetAxDataResult.Failure(AxDataErrors.StaleVersion, state.Version);
        }

        if (!MatchesDefinition(state, definition))
        {
            return SetAxDataResult.Failure(AxDataErrors.StructDefChanged);
        }

        var metadataError = ValidateMetadata(command, state, definition, permission);
        if (metadataError is not null)
        {
            return SetAxDataResult.Failure(metadataError);
        }

        var changes = new List<FieldChange>();
        var errors = new List<FieldEvaluationError>();
        var createdRows = new List<SetCreatedRow>();
        var changedParents = new List<FieldAddress>();
        var hasMutations = false;
        var fieldEvaluationStarted = Stopwatch.GetTimestamp();
        var preparedRows = new List<PreparedRow>();
        foreach (var containerDefinition in definition.DataContainers.Where(definition =>
                     command.DataContainers.Any(candidate =>
                         string.Equals(candidate.Name, definition.Name, StringComparison.OrdinalIgnoreCase))))
        {
            var submittedContainer = command.DataContainers.Single(candidate =>
                string.Equals(candidate.Name, containerDefinition.Name, StringComparison.OrdinalIgnoreCase));
            var container = state.DataContainers[submittedContainer.Name];

            for (var submittedRowIndex = 0; submittedRowIndex < submittedContainer.Rows.Count; submittedRowIndex++)
            {
                var submittedRow = submittedContainer.Rows[submittedRowIndex];
                var row = ResolveRow(container, containerDefinition, submittedRow, rowIdGenerator);
                var isNewRow = submittedRow.Action == AxRowAction.New;
                if (submittedRow.Action == AxRowAction.Delete)
                {
                    container.ApplyRowState(row.RowId, AxRowState.Deleted);
                    hasMutations = true;
                    continue;
                }

                preparedRows.Add(new PreparedRow(
                    container,
                    row,
                    submittedRow,
                    submittedRowIndex,
                    submittedRow.Action == AxRowAction.New,
                    OrderFields(submittedRow, containerDefinition).ToArray()));
            }
        }

        var submissions = preparedRows
            .SelectMany(prepared => prepared.Fields.Select(field => new PreparedField(
                prepared,
                new FieldAddress(prepared.Container.Name, prepared.Row.RowId, field.Name),
                field.Value)))
            .ToArray();
        if (submissions.Length > 0)
        {
            var started = await fieldEngine.CreateOperationAsync(state, actor, cancellationToken).ConfigureAwait(false);
            if (!started.Succeeded || started.Operation is null)
            {
                if (started.FailureKind == FieldOperationFailureKind.DefinitionChanged)
                {
                    return SetAxDataResult.Failure(AxDataErrors.StructDefChanged);
                }

                errors.Add(new FieldEvaluationError(
                    submissions[0].Address,
                    AxDataErrors.ValidationFailed.Code,
                    started.Error ?? AxDataErrors.ValidationFailed.Message));
            }
            else
            {
                await using var operation = started.Operation;
                var rejectedSources = new List<FieldAddress>();
                for (var submissionIndex = 0; submissionIndex < submissions.Length; submissionIndex++)
                {
                    var submission = submissions[submissionIndex];
                    if (IsBlockedByRejectedSource(submission.Address, rejectedSources, definition))
                    {
                        continue;
                    }

                    var evaluation = await operation.EvaluateAsync(
                        submission.Address,
                        submission.Value,
                        cancellationToken).ConfigureAwait(false);
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
                                definition));
                        if (hasEligibleContinuation)
                        {
                            var rebuilt = await operation.RebuildAsync(cancellationToken).ConfigureAwait(false);
                            if (!rebuilt.Succeeded)
                            {
                                return SetAxDataResult.Failure(
                                    rebuilt.FailureKind == FieldOperationFailureKind.DefinitionChanged
                                        ? AxDataErrors.StructDefChanged
                                        : AxDataErrors.DependencyUnavailable);
                            }
                        }

                        continue;
                    }

                    submission.PreparedRow.HasValidField = true;
                }

                if (preparedRows.Any(row => row.HasValidField))
                {
                    var completed = await operation.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    if (!completed.Succeeded)
                    {
                        return SetAxDataResult.Failure(completed.Errors.Any(error =>
                            string.Equals(error.Code, AxDataErrors.StructDefChanged.Code, StringComparison.Ordinal))
                            ? AxDataErrors.StructDefChanged
                            : AxDataErrors.DependencyUnavailable);
                    }

                    changedParents.AddRange(completed.Changes.Select(change => change.Address));
                    if (command.GetDeps)
                    {
                        changes.AddRange(completed.Changes.Where(change =>
                            change.Source != FieldChangeSource.Submitted));
                    }
                }
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

            if (prepared.Submitted.Action == AxRowAction.Edit && prepared.HasValidField)
            {
                prepared.Row.TransitionTo(AxRowState.Edited);
            }

            hasMutations |= prepared.HasValidField;
        }

        var fieldEvaluationDuration = Stopwatch.GetElapsedTime(fieldEvaluationStarted).TotalMilliseconds;
        Log.Write(new OperationalEvent(
            "validation.completed", "set", errors.Count == 0 ? "succeeded" : "validation-errors",
            errors.Count == 0 ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId, Version: state.Version, ErrorCode: errors.Count == 0 ? null : AxDataErrors.ValidationFailed.Code,
            Count: errors.Count, DurationMs: fieldEvaluationDuration));
        Log.Write(new OperationalEvent(
            "dependency.evaluation.completed", "set", "completed",
            Detail: OperationalLogDetail.Diagnostic, CorrelationId: command.CorrelationId,
            TenantId: actor.TenantId, TransId: state.TransId, ObjectId: state.ObjectId, Version: state.Version,
            Count: changedParents.Count, DurationMs: fieldEvaluationDuration));

        if (!hasMutations)
        {
            return SetAxDataResult.Success(new SetAxDataResponse(state.ObjectId, state.Version, createdRows, changes, errors));
        }

        var fillGridStarted = Stopwatch.GetTimestamp();
        var fillGrid = await fillGridEngine.ExecuteApplicableAsync(
            state,
            changedParents,
            actor,
            cancellationToken).ConfigureAwait(false);
        var fillGridChanges = new List<FieldChange>();
        if (!fillGrid.Succeeded)
        {
            LogFillGrid("failed", OperationalLogLevel.Warning, 0, 0);
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        if (fillGrid.Operations.Any(operation =>
                !operation.Succeeded || !CanApplyFillGrid(state, definition, operation)))
        {
            LogFillGrid("invalid-result", OperationalLogLevel.Warning, fillGrid.Operations.Count, 0);
            return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        foreach (var operation in fillGrid.Operations)
        {
            if (!operation.Succeeded ||
                !TryApplyFillGrid(state, definition, operation, rowIdGenerator, out var operationChanges))
            {
                LogFillGrid("invalid-result", OperationalLogLevel.Warning, fillGrid.Operations.Count, fillGridChanges.Count);
                return SetAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
            }

            fillGridChanges.AddRange(operationChanges);
        }

        if (command.GetDeps)
        {
            changes.AddRange(fillGridChanges);
        }

        LogFillGrid("succeeded", OperationalLogLevel.Information, fillGrid.Operations.Count, fillGridChanges.Count);

        state.RegisterMutation();
        var envelope = new SnapshotEnvelope(
            Guid.NewGuid().ToString("N"),
            state.TenantId,
            state.ObjectId,
            state.Version,
            state,
            clock.UtcNow,
            command.CorrelationId ?? string.Empty);
        var publishStarted = Stopwatch.GetTimestamp();
        var write = await snapshotTransport.PublishAsync(
            envelope,
            command.ExpectedVersion,
            cancellationToken).ConfigureAwait(false);
        Log.Write(new OperationalEvent(
            "snapshot.publish.completed", "set",
            write.Status == AxDataStoreWriteStatus.Stored ? "stored" : "stale",
            write.Status == AxDataStoreWriteStatus.Stored ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            CorrelationId: command.CorrelationId, TenantId: actor.TenantId, TransId: state.TransId,
            ObjectId: state.ObjectId, Version: state.Version, CurrentVersion: write.CurrentVersion,
            DurationMs: Stopwatch.GetElapsedTime(publishStarted).TotalMilliseconds));
        if (write.Status == AxDataStoreWriteStatus.StaleVersion)
        {
            return SetAxDataResult.Failure(AxDataErrors.StaleVersion, write.CurrentVersion);
        }

        return SetAxDataResult.Success(new SetAxDataResponse(state.ObjectId, state.Version, createdRows, changes, errors));

        void LogFillGrid(string outcome, OperationalLogLevel level, int operationCount, int changeCount) =>
            Log.Write(new OperationalEvent(
                "fillgrid.completed", "set", outcome, level, OperationalLogDetail.Diagnostic,
                command.CorrelationId, actor.TenantId, state.TransId, state.ObjectId, state.Version,
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

    private static bool MatchesDefinition(AxDataState state, StructDefHandle definition) =>
        string.Equals(state.TransId, definition.TransId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(state.StructDefVersion, definition.Version, StringComparison.OrdinalIgnoreCase);

    private static AxDataErrorDefinition? ValidateMetadata(
        SetAxDataCommand command,
        AxDataState state,
        StructDefHandle definition,
        AxQueryPermission permission)
    {
        foreach (var submittedContainer in command.DataContainers)
        {
            var containerDefinition = definition.DataContainers.SingleOrDefault(candidate =>
                string.Equals(candidate.Name, submittedContainer.Name, StringComparison.OrdinalIgnoreCase));
            if (containerDefinition is null || !state.DataContainers.TryGetValue(submittedContainer.Name, out var container))
            {
                return AxDataErrors.InvalidRequest;
            }

            if (!CanWriteContainer(submittedContainer.Name, permission))
            {
                return AxDataErrors.AccessDenied;
            }

            foreach (var submittedRow in submittedContainer.Rows)
            {
                var existing = string.IsNullOrWhiteSpace(submittedRow.RowId)
                    ? null
                    : container.Rows.SingleOrDefault(row =>
                        string.Equals(row.RowId, submittedRow.RowId, StringComparison.OrdinalIgnoreCase));
                if (submittedRow.Action == AxRowAction.New)
                {
                    if (!string.IsNullOrWhiteSpace(submittedRow.RowId) || existing is not null ||
                        !container.IsGrid && container.Rows.Count != 0)
                    {
                        return AxDataErrors.InvalidRequest;
                    }
                }
                else if (existing is null || existing.PersistedRecordId is null ||
                         existing.State == AxRowState.Deleted)
                {
                    return AxDataErrors.InvalidRequest;
                }

                if (submittedRow.Action == AxRowAction.Delete && submittedRow.Fields.Count != 0)
                {
                    return AxDataErrors.InvalidRequest;
                }

                foreach (var submittedField in submittedRow.Fields)
                {
                    var fieldDefinition = containerDefinition.Fields.SingleOrDefault(field =>
                        string.Equals(field.Name, submittedField.Name, StringComparison.OrdinalIgnoreCase));
                    if (fieldDefinition is null)
                    {
                        return AxDataErrors.InvalidRequest;
                    }

                    if (fieldDefinition.IsReadOnly ||
                        existing?.Fields.TryGetValue(submittedField.Name, out var field) == true && field.IsReadOnly ||
                        IsReadOnlyField(submittedContainer.Name, submittedField.Name, permission) ||
                        !IsIncludedField(submittedContainer.Name, submittedField.Name, permission))
                    {
                        return AxDataErrors.AccessDenied;
                    }
                }
            }
        }

        return null;
    }

    private static AxDataRow ResolveRow(
        AxDataContainer container,
        DataContainerDefinition definition,
        SetRowValue submitted,
        IRowIdGenerator rowIdGenerator)
    {
        var row = string.IsNullOrWhiteSpace(submitted.RowId)
            ? null
            : container.Rows.SingleOrDefault(candidate =>
                string.Equals(candidate.RowId, submitted.RowId, StringComparison.OrdinalIgnoreCase));
        if (row is not null)
        {
            return row;
        }

        var fields = definition.Fields.ToDictionary(
            field => field.Name,
            field => new AxFieldValue(null, field.IsReadOnly),
            StringComparer.OrdinalIgnoreCase);
        row = new AxDataRow(NewUniqueRowId(container, [], rowIdGenerator), AxRowState.New, fields: fields);
        container.AddRow(row);
        return row;
    }

    private static IEnumerable<SetFieldValue> OrderFields(
        SetRowValue submittedRow,
        DataContainerDefinition containerDefinition)
    {
        var orderedNames = containerDefinition.EvaluationOrder ?? containerDefinition.Fields.Select(field => field.Name).ToArray();
        return orderedNames
            .Where(name => submittedRow.Fields.Any(field =>
                string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => submittedRow.Fields.Single(field =>
                string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsBlockedByRejectedSource(
        FieldAddress candidate,
        IReadOnlyCollection<FieldAddress> rejectedSources,
        StructDefHandle definition) =>
        rejectedSources.Any(rejected =>
            string.Equals(rejected.RowId, candidate.RowId, StringComparison.OrdinalIgnoreCase) &&
            definition.Dependencies.Any(dependency =>
                string.Equals(dependency.SourceContainer, rejected.DataContainer, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dependency.SourceField, rejected.FieldName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dependency.TargetContainer, candidate.DataContainer, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(dependency.TargetField, candidate.FieldName, StringComparison.OrdinalIgnoreCase)));

    private static bool TryApplyFillGrid(
        AxDataState state,
        StructDefHandle definition,
        FillGridResult fillGrid,
        IRowIdGenerator rowIdGenerator,
        out IReadOnlyList<FieldChange> changes)
    {
        changes = [];
        if (!CanApplyFillGrid(state, definition, fillGrid))
        {
            return false;
        }

        var containerDefinition = definition.DataContainers.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, fillGrid.TargetContainer, StringComparison.OrdinalIgnoreCase));
        var container = state.DataContainers[containerDefinition!.Name];

        var generatedRows = new List<AxDataRow>();
        var generatedChanges = new List<FieldChange>();
        foreach (var fillGridRow in fillGrid.Rows)
        {
            var id = NewUniqueRowId(container, generatedRows, rowIdGenerator);

            var fields = containerDefinition.Fields.ToDictionary(
                field => field.Name,
                field => new AxFieldValue(
                    fillGridRow.Values.TryGetValue(field.Name, out var value) ? value : null,
                    field.IsReadOnly),
                StringComparer.OrdinalIgnoreCase);
            generatedRows.Add(new AxDataRow(id, AxRowState.New, fields: fields));
            generatedChanges.AddRange(fillGridRow.Values.Select(value => new FieldChange(
                new FieldAddress(container.Name, id, value.Key),
                value.Value,
                FieldChangeSource.FillGrid)));
        }

        if (fillGrid.Mode == FillGridMode.Replace)
        {
            container.Rows.Clear();
        }

        foreach (var row in generatedRows)
        {
            container.AddRow(row);
        }

        changes = generatedChanges;
        return true;
    }

    private static bool CanApplyFillGrid(
        AxDataState state,
        StructDefHandle definition,
        FillGridResult fillGrid)
    {
        var containerDefinition = definition.DataContainers.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, fillGrid.TargetContainer, StringComparison.OrdinalIgnoreCase));
        if (containerDefinition is null || !containerDefinition.IsGrid ||
            !state.DataContainers.TryGetValue(containerDefinition.Name, out var container) || !container.IsGrid)
        {
            return false;
        }

        var knownFields = containerDefinition.Fields
            .Select(field => field.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return fillGrid.Rows.All(row => row.Values.Keys.All(knownFields.Contains));
    }

    private static bool IsValid(SetAxDataCommand command) =>
        !string.IsNullOrWhiteSpace(command.ObjectId) &&
        command.ExpectedVersion > 0 &&
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

    private sealed record PreparedField(PreparedRow PreparedRow, FieldAddress Address, object? Value);
}
