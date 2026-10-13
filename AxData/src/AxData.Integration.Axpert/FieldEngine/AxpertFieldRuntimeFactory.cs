using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using AxValidate;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed class AxpertFieldRuntimeFactory(
    IAxpertRuntimeContextFactory contextFactory,
    IAxpertStructDefSource structDefSource) : IAxpertFieldRuntimeFactory
{
    public async Task<AxpertOperationResult<AxpertFieldRuntime>> CreateAsync(
        AxpertFieldRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = await structDefSource.LoadCurrentAsync(
            request.TenantId,
            request.TransId,
            cancellationToken).ConfigureAwait(false);
        if (current is null ||
            !string.Equals(current.ContentHash, request.PublishedDefinitionVersion, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The current StructDef content hash does not match the AxData snapshot.");
        }

        var runtimeResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!runtimeResult.Succeeded || runtimeResult.Value is null)
        {
            return Failure(runtimeResult.Error ?? "The Axpert runtime could not be created.");
        }

        try
        {
            var context = runtimeResult.Value;
            var definition = await LegacyStructDef.LoadStructure(
                context.Common,
                request.TransId,
                context.Roles.ToList(),
                context.TraceService,
                refreshCache: true).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (definition?.Frames is null)
            {
                return Failure("StructDef.LoadStructure returned no definition.");
            }

            var validate = new Validate(context.Common, context.TraceService)
            {
                _common = context.Common,
                _sdef = definition,
                _dbType = context.Common._dbType
            };
            if (!await validate.InitValidateAsync(request.TransId).ConfigureAwait(false))
            {
                return Failure("The Axpert Validate/Parser runtime could not be initialized.");
            }

            var frames = definition.Frames.ToDictionary(
                frame => $"dc{frame.FrameNo}",
                frame => frame.FrameNo,
                StringComparer.OrdinalIgnoreCase);
            return new AxpertOperationResult<AxpertFieldRuntime>(
                true,
                new AxpertFieldRuntime(validate, frames),
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("The Axpert Validate/Parser runtime could not be initialized.");
        }
    }

    private static AxpertOperationResult<AxpertFieldRuntime> Failure(string error) =>
        new(false, null!, error);
}
