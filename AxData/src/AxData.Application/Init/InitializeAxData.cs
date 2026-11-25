using System.Diagnostics;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Model;

namespace AxData.Application.Init;

public sealed record InitializeAxDataCommand(string TransId, AxRecordKey? Key);

public sealed record InitializeAxDataResponse(
    string ObjectId,
    string TransId,
    long? RecordId,
    bool ReadOnly);

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
    public int ObjectIdCollisionRetries { get; init; } = 3;
}

public sealed class PortBasedAxpertInitializer(
    IStructDefProvider structDefProvider,
    IAxpertDataLoader dataLoader,
    IAxpertFieldEngine fieldEngine) : IAxpertInitializer
{
    /// <summary>Loads the transaction definition and state, runs load-time Axpert rules, and accepts the resulting baseline.</summary>
    /// <returns>Initialized data on success; otherwise a categorized dependency, transaction, or validation failure.</returns>
    public async Task<AxpertInitializationResult> InitializeAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        var definition = await structDefProvider.GetAsync(
            tenantId,
            transId,
            cancellationToken);
        if (definition is null)
        {
            return AxpertInitializationResult.Failure(
                AxpertInitializationFailureKind.UnknownTransaction);
        }

        var loaded = await dataLoader.LoadAsync(definition, key, user, cancellationToken);
        if (!loaded.Succeeded || loaded.State is null)
        {
            return AxpertInitializationResult.Failure(
                AxpertInitializationFailureKind.DependencyUnavailable,
                loaded.Error);
        }

        var fieldOperation = await fieldEngine.CreateOperationAsync(
            loaded.State,
            user,
            cancellationToken);
        if (!fieldOperation.Succeeded || fieldOperation.Operation is null)
        {
            return AxpertInitializationResult.Failure(
                AxpertInitializationFailureKind.DependencyUnavailable,
                fieldOperation.Error);
        }

        await using (fieldOperation.Operation)
        {
            var initialized = await fieldOperation.Operation.InitializeOnLoadAsync(cancellationToken);
            if (!initialized.Succeeded)
            {
                return AxpertInitializationResult.Failure(
                    AxpertInitializationFailureKind.ValidationFailed,
                    initialized.Errors.FirstOrDefault()?.Message);
            }

            AcceptBaseline(loaded.State);
            return AxpertInitializationResult.Success(AxpertInitializedData.FromState(loaded.State));
        }
    }

    /// <summary>Marks every initialized field as the unchanged baseline for subsequent mutation tracking.</summary>
    private static void AcceptBaseline(AxDataState state)
    {
        foreach (var field in state.DataContainers.Values
                     .SelectMany(container => container.Rows)
                     .SelectMany(row => row.Fields.Values))
        {
            field.AcceptChanges();
        }
    }
}

public sealed class InitializeAxDataHandler(
    IUserContext userContext,
    IAxpertAuthorizationProvider authorizationProvider,
    IAxpertInitializer initializer,
    IAxDataStore store,
    IObjectIdGenerator objectIdGenerator,
    IClock clock,
    InitializeAxDataOptions options,
    IOperationalEventSink? operationalLog = null)
{
    /// <summary>Authorizes and initializes a transaction, assigns an opaque object ID, and stores it tenant-scoped.</summary>
    /// <returns>A reference to the initialized object, or a safe application error.</returns>
    /// <remarks>Authorization precedes initialization and storage. Object-ID collisions are retried with newly generated IDs.</remarks>
    public async Task<InitializeAxDataResult> HandleAsync(
        InitializeAxDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.TransId) || !IsValidKey(command.Key))
        {
            return InitializeAxDataResult.Failure(AxDataErrors.InvalidRequest);
        }

        var user = await userContext.GetCurrentAsync(cancellationToken);
        var authorizationStarted = Stopwatch.GetTimestamp();
        var permission = await authorizationProvider.GetCreatePermissionAsync(
            user.TenantId,
            command.TransId,
            user,
            cancellationToken);
        var isAuthorized = permission.IsAllowed;
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

        var key = command.Key ?? new AxRecordKey(null, null, null);
        var initialized = await initializer.InitializeAsync(
            user.TenantId,
            command.TransId,
            key,
            user,
            cancellationToken);
        if (!initialized.Succeeded || initialized.Data is null)
        {
            return InitializeAxDataResult.Failure(initialized.FailureKind switch
            {
                AxpertInitializationFailureKind.UnknownTransaction => AxDataErrors.InvalidRequest,
                AxpertInitializationFailureKind.ValidationFailed => AxDataErrors.ValidationFailed,
                _ => AxDataErrors.DependencyUnavailable
            });
        }

        ValidateOptions();
        var now = clock.UtcNow;
        var state = CreateState(
            initialized.Data,
            user,
            objectIdGenerator.NewObjectId(),
            now);
        Log.Write(new OperationalEvent(
            "structdef.binding.bypassed", "initialize", "current-definition",
            Detail: OperationalLogDetail.Diagnostic,
            CorrelationId: Activity.Current?.TraceId.ToString(),
            TenantId: user.TenantId, TransId: state.TransId, ObjectId: state.ObjectId));

        for (var attempt = 0; attempt < options.ObjectIdCollisionRetries; attempt++)
        {
            var write = await store.WriteAsync(state, lockToken: null, cancellationToken);
            Log.Write(new OperationalEvent(
                "redis.create.completed", "initialize", write.Status == AxDataStoreWriteStatus.Stored ? "stored" : "conflict",
                write.Status == AxDataStoreWriteStatus.Stored ? OperationalLogLevel.Information : OperationalLogLevel.Warning,
                TenantId: user.TenantId, TransId: state.TransId, ObjectId: state.ObjectId,
                Attempt: attempt + 1));
            if (write.Status == AxDataStoreWriteStatus.Stored)
            {
                return InitializeAxDataResult.Success(new InitializeAxDataResponse(
                    state.ObjectId,
                    state.TransId,
                    state.RecordId,
                    permission.IsReadOnly));
            }

            if (attempt + 1 < options.ObjectIdCollisionRetries)
            {
                now = clock.UtcNow;
                state = CreateState(
                    initialized.Data,
                    user,
                    objectIdGenerator.NewObjectId(),
                    now);
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
        if (options.ObjectIdCollisionRetries <= 0)
        {
            throw new InvalidOperationException("Initialize options must contain a positive retry count.");
        }
    }

    private static AxDataState CreateState(
        AxpertInitializedData initialized,
        AxUserContext user,
        string objectId,
        DateTimeOffset now)
    {
        var state = new AxDataState(
            objectId,
            user.TenantId,
            initialized.TransId,
            user.UserName,
            now)
        {
            RecordId = initialized.RecordId,
            AxOpen = initialized.AxOpen
        };

        foreach (var container in initialized.DataContainers)
        {
            state.DataContainers.Add(container.Name, container);
        }

        state.Files.AddRange(initialized.Files);
        return state;
    }
}
