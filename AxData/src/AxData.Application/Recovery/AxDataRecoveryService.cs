using System.Diagnostics;
using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.Application.Recovery;

public sealed record AxDataRecoveryResult(AxDataState? State, AxDataErrorDefinition? Error)
{
    public bool Succeeded => State is not null;

    public static AxDataRecoveryResult Success(AxDataState state) => new(state, null);

    public static AxDataRecoveryResult Failure(AxDataErrorDefinition error) => new(null, error);
}

public sealed class AxDataRecoveryService(
    IAxDataStore activeStore,
    IAxDataSnapshotRepository snapshotRepository,
    IStructDefProvider structDefProvider,
    IAxpertAuthorizationProvider authorizationProvider,
    IOperationalEventSink? operationalLog = null)
{
    public async Task<AxDataRecoveryResult> RecoverAsync(
        AxUserContext actor,
        string objectId,
        AxAuthorizationOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);

        var loadStarted = Stopwatch.GetTimestamp();
        var read = await snapshotRepository.LoadLatestAsync(
            actor.TenantId,
            objectId,
            cancellationToken).ConfigureAwait(false);
        Log.Write(new OperationalEvent(
            "snapshot.recovery.load.completed", "recovery", read.Status.ToString().ToLowerInvariant(),
            read.Status is SnapshotReadStatus.Unavailable or SnapshotReadStatus.Invalid
                ? OperationalLogLevel.Warning
                : OperationalLogLevel.Information,
            TenantId: actor.TenantId, ObjectId: objectId, Version: read.State?.Version,
            DurationMs: Stopwatch.GetElapsedTime(loadStarted).TotalMilliseconds));

        if (read.Status == SnapshotReadStatus.NotFound)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.ObjectNotFound);
        }

        if (read.Status == SnapshotReadStatus.Unavailable)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        if (read.Status != SnapshotReadStatus.Found || read.State is null ||
            !string.Equals(read.State.TenantId, actor.TenantId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(read.State.ObjectId, objectId, StringComparison.OrdinalIgnoreCase) ||
            read.State.Version < 1)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.RecoveryFailed);
        }

        var recovered = read.State;
        if (recovered.Status == AxDataLifecycleStatus.Expired)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.ObjectExpired);
        }

        if (recovered.Status == AxDataLifecycleStatus.Closed)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.ObjectNotFound);
        }

        var permission = operation switch
        {
            AxAuthorizationOperation.Query => await authorizationProvider.GetQueryPermissionAsync(
                actor.TenantId, recovered.TransId, new AxRecordKey(recovered.KeyField, recovered.KeyValue, recovered.RecordId), actor, cancellationToken).ConfigureAwait(false),
            AxAuthorizationOperation.Set => await authorizationProvider.GetSetPermissionAsync(
                actor.TenantId, recovered.TransId, new AxRecordKey(recovered.KeyField, recovered.KeyValue, recovered.RecordId), actor, cancellationToken).ConfigureAwait(false),
            _ => null
        };
        var isAuthorized = permission is not null && IsAuthorized(actor, recovered, permission, operation);
        Log.Write(new OperationalEvent(
            "authorization.completed", "recovery", isAuthorized ? "allowed" : "denied",
            isAuthorized ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            TenantId: actor.TenantId, TransId: recovered.TransId, ObjectId: recovered.ObjectId, Version: recovered.Version,
            ReasonCode: isAuthorized ? "policy-allowed" : "policy-denied"));
        if (!isAuthorized)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.AccessDenied);
        }

        var definition = await structDefProvider.GetAsync(
            actor.TenantId,
            recovered.TransId,
            cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.RecoveryFailed);
        }

        if (!string.Equals(definition.Version, recovered.StructDefVersion, StringComparison.OrdinalIgnoreCase))
        {
            return AxDataRecoveryResult.Failure(AxDataErrors.StructDefChanged);
        }

        var restore = await activeStore.WriteAsync(recovered, expectedVersion: null, cancellationToken).ConfigureAwait(false);
        Log.Write(new OperationalEvent(
            "redis.recovery.completed", "recovery",
            restore.Status == AxDataStoreWriteStatus.Stored ? "restored" : "concurrent",
            restore.Status == AxDataStoreWriteStatus.Stored ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            TenantId: actor.TenantId, TransId: recovered.TransId, ObjectId: recovered.ObjectId,
            Version: recovered.Version, CurrentVersion: restore.CurrentVersion));
        if (restore.Status == AxDataStoreWriteStatus.Stored)
        {
            return AxDataRecoveryResult.Success(recovered);
        }

        var concurrent = await activeStore.LoadAsync(actor.TenantId, objectId, cancellationToken).ConfigureAwait(false);
        return concurrent is not null && concurrent.Version >= recovered.Version
            ? AxDataRecoveryResult.Success(concurrent)
            : AxDataRecoveryResult.Failure(AxDataErrors.RecoveryFailed);
    }

    private IOperationalEventSink Log => operationalLog ?? NullOperationalEventSink.Instance;

    private static bool IsAuthorized(
        AxUserContext actor,
        AxDataState state,
        AxQueryPermission permission,
        AxAuthorizationOperation operation)
    {
        var operationName = operation.ToString().ToLowerInvariant();
        var context = AxAuthorizationContext.FromState(
            actor,
            state,
            hasViewerAccess: permission.IsAllowed,
            hasOperationPermission: permission.IsAllowed && permission.AllowedOperations.Contains(operationName),
            isEditable: permission.CanEditTransaction);
        return AxAuthorizationPolicy.IsAllowed(context, operation);
    }
}
