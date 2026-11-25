using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.AxGet;
using AxData.Integration.Axpert.FieldEngine;
using AxData.Integration.Axpert.StructDef;

namespace AxData.Integration.Axpert.Initialization;

public sealed class AxpertInitializer(
    IAxpertStructDefRuntimeSource structDefSource,
    AxpertDataLoader dataLoader) : IAxpertInitializer
{
    public async Task<AxpertInitializationResult> InitializeAsync(
        string tenantId,
        string transId,
        AxRecordKey key,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var loadedDefinition = await structDefSource.LoadCurrentRuntimeAsync(
                tenantId,
                transId,
                cancellationToken);
            if (loadedDefinition is null)
            {
                return Failure(AxpertInitializationFailureKind.UnknownTransaction);
            }

            var sourceRequest = new AxpertDataSourceRequest(
                tenantId,
                transId,
                key,
                user.UserName,
                user.Roles);
            var isNew = key.RecordId is null &&
                        string.IsNullOrWhiteSpace(key.KeyField) &&
                        string.IsNullOrWhiteSpace(key.KeyValue);

            long? loadedRecordId;
            IReadOnlyList<AxpertLoadedContainerShape> loadedContainers;
            AxpertOperationResult<AxpertFieldRuntime> runtimeResult;
            if (isNew)
            {
                loadedRecordId = null;
                loadedContainers = [];
                runtimeResult = await AxpertFieldRuntimeFactory.CreateFromLoadedAsync(
                    loadedDefinition.Context,
                    loadedDefinition.Definition,
                    transId,
                    recordId: 0,
                    cancellationToken);
                if (!runtimeResult.Succeeded || runtimeResult.Value is null ||
                    !PrepareNewRuntime(runtimeResult.Value, cancellationToken))
                {
                    return Failure(
                        AxpertInitializationFailureKind.DependencyUnavailable,
                        runtimeResult.Error ?? "The new Axpert runtime could not be prepared.");
                }
            }
            else
            {
                var axGetService = AxpertDataSource.CreateService(loadedDefinition.Context);
                var input = AxpertDataSource.CreateInput(sourceRequest, loadedDefinition.Context);
                var access = await axGetService.CheckTstructViewAccess(input);
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(access.error) ||
                    !AxpertDataSource.TryInt64(access.data, "RecordId", out var recordId))
                {
                    return Failure(AxpertInitializationFailureKind.DependencyUnavailable);
                }

                runtimeResult = await AxpertFieldRuntimeFactory.CreateFromLoadedAsync(
                    loadedDefinition.Context,
                    loadedDefinition.Definition,
                    transId,
                    recordId,
                    cancellationToken);
                if (!runtimeResult.Succeeded || runtimeResult.Value is null)
                {
                    return Failure(AxpertInitializationFailureKind.DependencyUnavailable);
                }

                input.RecordId = recordId;
                var loaded = await axGetService.LoadBaselineData(
                    input,
                    access,
                    runtimeResult.Value.Validate);
                cancellationToken.ThrowIfCancellationRequested();
                if (!loaded.success || !string.IsNullOrWhiteSpace(loaded.error) ||
                    !loaded.data.TryGetValue("data", out var payload))
                {
                    return Failure(AxpertInitializationFailureKind.DependencyUnavailable);
                }

                loadedRecordId = AxpertDataSource.TryInt64(loaded.data, "recordid", out var returnedRecordId)
                    ? returnedRecordId
                    : recordId;
                loadedContainers = AxpertDataSource.ProjectLoadShape(payload);
            }

            await runtimeResult.Value.Validate.InitializeFieldsOnLoad(isNew);
            cancellationToken.ThrowIfCancellationRequested();
            if (!runtimeResult.Value.Validate.validationPassed)
            {
                return Failure(
                    AxpertInitializationFailureKind.ValidationFailed,
                    "Axpert rejected on-load field processing.");
            }

            var initialized = dataLoader.BuildInitializedData(
                loadedDefinition.Definition,
                transId,
                loadedRecordId,
                loadedContainers);
            return AxpertInitializationResult.Success(initialized);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure(AxpertInitializationFailureKind.DependencyUnavailable);
        }
    }

    private static AxpertInitializationResult Failure(
        AxpertInitializationFailureKind failureKind,
        string? error = null) =>
        AxpertInitializationResult.Failure(failureKind, error);

    private static bool PrepareNewRuntime(
        AxpertFieldRuntime runtime,
        CancellationToken cancellationToken)
    {
        var definition = runtime.Validate._sdef;
        var parser = runtime.Validate._parser;
        if (definition?.Frames is null || definition.Flds is null || parser is null)
        {
            return false;
        }

        foreach (var frame in definition.Frames.Where(frame => !frame.AsGrid).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = definition.Flds
                .Where(field => field.FrameNo == frame.FrameNo)
                .ToArray();
            foreach (var field in fields)
            {
                if (!definition.SubmitValueOnLoad(field.FieldName, rowNo: 1, string.Empty))
                {
                    return false;
                }

                parser.RegisterVar(field.FieldName, field.DataType, string.Empty);
            }
        }

        return true;
    }
}
