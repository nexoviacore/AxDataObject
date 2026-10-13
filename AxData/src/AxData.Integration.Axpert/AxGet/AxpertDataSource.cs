using System.Globalization;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Runtime;
using AxData.Integration.Axpert.StructDef;
using AxTstructData;
using Newtonsoft.Json;

namespace AxData.Integration.Axpert.AxGet;

public sealed class AxpertDataSource(
    IAxpertRuntimeContextFactory contextFactory,
    IAxpertStructDefSource structDefSource) : IAxpertDataSource
{
    public async Task<AxpertDataSourceResult> LoadAsync(
        AxpertDataSourceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await structDefSource.LoadCurrentAsync(
            request.TenantId,
            request.TransId,
            cancellationToken).ConfigureAwait(false);
        if (current is null ||
            !string.Equals(
                current.ContentHash,
                request.PublishedDefinitionVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The current StructDef content hash does not match the AxData snapshot.");
        }

        var runtimeResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!runtimeResult.Succeeded || runtimeResult.Value is null)
        {
            return Failure(runtimeResult.Error ?? "The Axpert runtime could not be created.");
        }

        if (request.Key.RecordId is null &&
            string.IsNullOrWhiteSpace(request.Key.KeyField) &&
            string.IsNullOrWhiteSpace(request.Key.KeyValue))
        {
            return new AxpertDataSourceResult(true, null, []);
        }

        try
        {
            var runtime = runtimeResult.Value;
            var repository = new AxGetRepository(runtime.Common, runtime.TraceService);
            var attachmentsRepository = new AxAttachmentsRepository(runtime.Common);
            var attachmentsService = new AxAttachmentsService(
                runtime.Common,
                attachmentsRepository,
                runtime.TraceService);
            var axGetService = new AxGetService(
                repository,
                runtime.Common,
                runtime.TraceService,
                attachmentsService,
                new AxParserProvider(runtime.Common, runtime.TraceService));
            var input = new AxTstructData.AxGet
            {
                ARMSessionId = runtime.SessionId,
                TransId = request.TransId,
                RecordId = request.Key.RecordId ?? 0,
                KeyField = AxpertRecordKeyMapping.Field(request.Key),
                KeyValue = AxpertRecordKeyMapping.Value(request.Key),
                Roles = request.Roles.ToList(),
                UserName = request.UserName,
                Project = request.TenantId,
                Action = "view",
                Trace = false,
                AxpertWSFormat = false,
                AxClient_DateFormat = runtime.Common.ApiDateFormat,
                MilliSecsInTimeStamp = runtime.Common._milliSecsInTimeStamp
            };
            input.Dimensions = runtime.Common.GetAxgApplicableDimensionsString();

            var access = await axGetService.CheckTstructViewAccess(input).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(access.error) ||
                !TryInt64(access.data, "RecordId", out var recordId))
            {
                return Failure(access.error ?? "AxGet view permission or record resolution failed.");
            }

            input.RecordId = recordId;
            var loaded = await axGetService.LoadData(input, access).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!loaded.success || !string.IsNullOrWhiteSpace(loaded.error) ||
                !loaded.data.TryGetValue("data", out var payload))
            {
                return Failure(loaded.error ?? "AxGet returned no data payload.");
            }

            return new AxpertDataSourceResult(
                true,
                TryInt64(loaded.data, "recordid", out var returnedRecordId) ? returnedRecordId : recordId,
                ProjectContainers(payload));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure("AxGet failed while loading the review record.");
        }
    }

    private static IReadOnlyList<AxpertDataContainerResult> ProjectContainers(object payload)
    {
        var normalized = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, object?>>>>(
            JsonConvert.SerializeObject(payload))
            ?? throw new InvalidOperationException("AxGet returned an invalid data payload.");

        return normalized.Select(container => new AxpertDataContainerResult(
            container.Key,
            container.Value.Select(row =>
            {
                var values = new Dictionary<string, object?>(row.Value, StringComparer.OrdinalIgnoreCase);
                long? recordId = null;
                if (values.Remove("axp_recid", out var rawRecordId) &&
                    long.TryParse(Convert.ToString(rawRecordId, CultureInfo.InvariantCulture), out var parsed))
                {
                    recordId = parsed;
                }

                foreach (var companion in values.Keys
                    .Where(name => name.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase))
                    .Where(name => values.ContainsKey(name[..^"_sourceid".Length]))
                    .ToArray())
                {
                    values.Remove(companion);
                }

                return new AxpertDataRowResult(recordId, values);
            }).ToArray())).ToArray();
    }

    private static bool TryInt64(
        IReadOnlyDictionary<string, object> values,
        string key,
        out long value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(
                    Convert.ToString(pair.Value, CultureInfo.InvariantCulture),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value);
            }
        }

        value = 0;
        return false;
    }

    private static AxpertDataSourceResult Failure(string error) => new(false, null, null, error);
}
