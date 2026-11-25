using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.FillGrid;
using AxData.Integration.Axpert.StructDef;
using ARMCommon.Helpers;
using AxParserApp;
using AxValidate;
using LegacyStructDef = AxStructDef.StructDef;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed class AxpertFieldRuntimeFactory(
    IAxpertRuntimeContextFactory contextFactory,
    AxpertFillGridQueryOptions? fillGridOptions = null) : IAxpertFieldRuntimeFactory
{
    public async Task<AxpertOperationResult<AxpertFieldRuntime>> CreateAsync(
        AxpertFieldRuntimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var runtimeResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken);
        if (!runtimeResult.Succeeded || runtimeResult.Value is null)
        {
            return Failure(runtimeResult.Error ?? "The Axpert runtime could not be created.");
        }

        var context = runtimeResult.Value;
        var definition = await LegacyStructDef.LoadStructure(
            context.Common,
            request.TransId,
            context.Roles.ToList(),
            context.TraceService,
            refreshCache: true);
        cancellationToken.ThrowIfCancellationRequested();
        if (definition?.Frames is null || definition.Flds is null)
        {
            return Failure("StructDef.LoadStructure returned no definition.");
        }

        var fieldRuntime = await CreateFromLoadedAsync(
            context,
            definition,
            request.TransId,
            request.RecordId.GetValueOrDefault(),
            cancellationToken);
        if (!fieldRuntime.Succeeded || fieldRuntime.Value is null)
        {
            return fieldRuntime;
        }

        var fillGridRuntime = await AxpertFillGridRuntimeFactory.CreateFromLoadedAsync(
            context,
            definition,
            request.TenantId,
            request.TransId,
            fillGridOptions ?? new AxpertFillGridQueryOptions(),
            cancellationToken);
        if (!fillGridRuntime.Succeeded || fillGridRuntime.Value is null)
        {
            return Failure(fillGridRuntime.Error ?? "The Axpert FillGrid runtime could not be created.");
        }

        fieldRuntime.Value.AttachSetMetadata(
            AxpertStructDefSource.Project(definition, request.TenantId, request.TransId),
            fillGridRuntime.Value);
        return fieldRuntime;
    }

    internal static async Task<AxpertOperationResult<AxpertFieldRuntime>> CreateFromLoadedAsync(
        AxpertRuntimeContext context,
        LegacyStructDef definition,
        string transId,
        long recordId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (definition.Frames is null || definition.Flds is null)
            {
                return Failure("StructDef.LoadStructure returned no definition.");
            }

            var validate = new Validate(context.Common, context.TraceService)
            {
                _common = context.Common,
                _sdef = definition,
                _dbType = context.Common._dbType,
                _parser = new AxParser(context.Common, definition)
            };
            await PrepareValidationAsync(
                validate,
                context.Common,
                recordId,
                cancellationToken);

            if (!await validate.InitValidateAsync(transId))
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

    internal static async Task PrepareValidationAsync(
        Validate validate,
        Utils common,
        long recordId,
        CancellationToken cancellationToken)
    {
        await common.RestoreSessionGlobalsToParser(validate._parser);
        cancellationToken.ThrowIfCancellationRequested();

        validate._parser.RegisterVar(
            "recordid",
            "n",
            recordId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        validate._sdef.LastSavedRecordid = recordId;
        validate.PrepareExps();

        if (string.IsNullOrWhiteSpace(common.ApiDateFormat))
        {
            throw new InvalidOperationException("The trusted API date format is unavailable.");
        }

        validate._sdef.clientDateFormat = common.ApiDateFormat;
    }

    private static AxpertOperationResult<AxpertFieldRuntime> Failure(string error) =>
        new(false, null!, error);
}
