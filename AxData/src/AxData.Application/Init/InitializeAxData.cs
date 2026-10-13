using System.Diagnostics;
using AxData.Application.Authorization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Model;

namespace AxData.Application.Init;

public sealed record InitializeAxDataCommand(string TransId, AxRecordKey? Key);

public sealed record InitializeAxDataResponse(
    string ObjectId,
    long Version,
    string TransId,
    long? RecordId,
    bool ReadOnly,
    DateTimeOffset ExpiresAt);

public sealed record InitializeAxDataResult(
    InitializeAxDataResponse? Response,
    AxDataErrorDefinition? Error)
{
    public bool Succeeded => Response is not null;

    public static InitializeAxDataResult Success(InitializeAxDataResponse response) => new(response, null);

    public static InitializeAxDataResult Failure(AxDataErrorDefinition error) => new(null, error);
}

public sealed record InitializeAxDataOptions
{
    public TimeSpan InactivityTtl { get; init; } = TimeSpan.FromMinutes(30);

    public int ObjectIdCollisionRetries { get; init; } = 3;
}

public sealed class InitializeAxDataHandler(
    IUserContext userContext,
    IAxpertAuthorizationProvider authorizationProvider,
    IStructDefProvider structDefProvider,
    IAxpertDataLoader dataLoader,
    IAxDataStore store,
    IObjectIdGenerator objectIdGenerator,
    IClock clock,
    InitializeAxDataOptions options,
    IOperationalEventSink? operationalLog = null)
{
    public async Task<InitializeAxDataResult> HandleAsync(
        InitializeAxDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.TransId) || !IsValidKey(command.Key))
        {
            return InitializeAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var user = await userContext.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var authorizationStarted = Stopwatch.GetTimestamp();
        var permission = await authorizationProvider.GetCreatePermissionAsync(
            user.TenantId,
            command.TransId,
            user,
            cancellationToken).ConfigureAwait(false);
        var authorization = new AxAuthorizationContext(
            tenantMatches: true,
            AxActorRelation.Unrelated,
            permission.IsAllowed,
            isEditable: !permission.IsReadOnly);
        var isAuthorized = AxAuthorizationPolicy.IsAllowed(authorization, AxAuthorizationOperation.Initialize);
        Log.Write(new OperationalEvent(
            "authorization.completed", "initialize", isAuthorized ? "allowed" : "denied",
            isAuthorized ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
            TenantId: user.TenantId, TransId: command.TransId,
            ReasonCode: isAuthorized ? "policy-allowed" : "policy-denied",
            DurationMs: Stopwatch.GetElapsedTime(authorizationStarted).TotalMilliseconds));
        if (!isAuthorized)
        {
            return InitializeAxDataResult.Failure(AxDataErrors.AccessDenied);
        }

        var definition = await structDefProvider.GetAsync(
            user.TenantId,
            command.TransId,
            cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return InitializeAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var key = command.Key ?? new AxRecordKey(null, null, null);
        var loaded = await dataLoader.LoadAsync(definition, key, user, cancellationToken).ConfigureAwait(false);
        if (!loaded.Succeeded || loaded.State is null)
        {
            return InitializeAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
        }

        ValidateOptions();
        for (var attempt = 0; attempt < options.ObjectIdCollisionRetries; attempt++)
        {
            var now = clock.UtcNow;
            var state = CreateState(loaded.State, definition, user, objectIdGenerator.NewObjectId(), now);
            state.ExpiresAtUtc = now.Add(options.InactivityTtl);
            var write = await store.WriteAsync(state, expectedVersion: null, cancellationToken).ConfigureAwait(false);
            Log.Write(new OperationalEvent(
                "redis.cas.completed", "initialize", write.Status == AxDataStoreWriteStatus.Stored ? "stored" : "conflict",
                write.Status == AxDataStoreWriteStatus.Stored ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
                TenantId: user.TenantId, TransId: state.TransId, ObjectId: state.ObjectId,
                Version: state.Version, CurrentVersion: write.CurrentVersion, Attempt: attempt + 1));
            if (write.Status == AxDataStoreWriteStatus.Stored)
            {
                return InitializeAxDataResult.Success(new InitializeAxDataResponse(
                    state.ObjectId,
                    state.Version,
                    state.TransId,
                    state.RecordId,
                    permission.IsReadOnly,
                    state.ExpiresAtUtc.Value));
            }
        }

        return InitializeAxDataResult.Failure(AxDataErrors.DependencyUnavailable);
    }

    private IOperationalEventSink Log => operationalLog ?? NullOperationalEventSink.Instance;

    private static bool IsValidKey(AxRecordKey? key) =>
        key is null ||
        (key.RecordId > 0 && key.KeyField is null && key.KeyValue is null) ||
        (key.RecordId is null && !string.IsNullOrWhiteSpace(key.KeyField) && !string.IsNullOrWhiteSpace(key.KeyValue));

    private void ValidateOptions()
    {
        if (options.InactivityTtl <= TimeSpan.Zero || options.ObjectIdCollisionRetries <= 0)
        {
            throw new InvalidOperationException("Initialize options must contain a positive TTL and retry count.");
        }
    }

    private static AxDataState CreateState(
        AxDataState loaded,
        StructDefHandle definition,
        AxUserContext user,
        string objectId,
        DateTimeOffset now)
    {
        var state = new AxDataState(
            objectId,
            user.TenantId,
            definition.TransId,
            user.UserName,
            user.UserName,
            definition.Version,
            now)
        {
            KeyField = loaded.KeyField,
            KeyValue = loaded.KeyValue,
            RecordId = loaded.RecordId,
            AxOpen = loaded.AxOpen
        };

        foreach (var container in loaded.DataContainers.Values)
        {
            state.DataContainers.Add(container.Name, container);
        }

        state.Files.AddRange(loaded.Files);
        return state;
    }
}
