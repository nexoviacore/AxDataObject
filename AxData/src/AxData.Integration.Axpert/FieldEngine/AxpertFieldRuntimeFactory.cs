using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using ARMCommon.Helpers;
using AxParserApp;
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
            return Failure(
                "The current StructDef content hash does not match the AxData snapshot.",
                definitionChanged: true);
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
                _dbType = context.Common._dbType,
                _parser = new AxParser(context.Common, definition)
            };
            await PrepareValidationAsync(
                validate,
                context.Common,
                request.RecordId.GetValueOrDefault(),
                cancellationToken).ConfigureAwait(false);

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

    internal static async Task PrepareValidationAsync(
        Validate validate,
        Utils common,
        long recordId,
        CancellationToken cancellationToken)
    {
        await common.RestoreSessionGlobalsToParser(validate._parser).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        validate._parser.RegisterVar(
            "recordid",
            "n",
            recordId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        validate._sdef.LastSavedRecordid = recordId;
        validate.PrepareExps();

        if (!string.IsNullOrWhiteSpace(common.ApiDateFormat))
        {
            validate._sdef.clientDateFormat = common.ApiDateFormat;
            return;
        }

        var globalDateFormat = validate._parser.GetVarValue("axclient_dateformat");
        if (!string.IsNullOrWhiteSpace(globalDateFormat))
        {
            validate._sdef.clientDateFormat = globalDateFormat;
        }
    }

    private static AxpertOperationResult<AxpertFieldRuntime> Failure(
        string error,
        bool definitionChanged = false) =>
        new(false, null!, error, definitionChanged);
}
