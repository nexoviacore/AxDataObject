using System.Collections;
using System.Globalization;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.Runtime;
using AxTstructData;

namespace AxData.Integration.Axpert.AxGet;

public sealed class AxpertDataSource(
    IAxpertRuntimeContextFactory contextFactory) : IAxpertDataSource
{
    public async Task<AxpertDataSourceResult> LoadAsync(
        AxpertDataSourceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var runtimeResult = await contextFactory.CreateAsync(request.TenantId, cancellationToken);
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
            var axGetService = CreateService(runtime);
            var input = CreateInput(request, runtime);

            var access = await axGetService.CheckTstructViewAccess(input);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(access.error) ||
                !TryInt64(access.data, "RecordId", out var recordId))
            {
                return Failure(access.error ?? "AxGet view permission or record resolution failed.");
            }

            input.RecordId = recordId;
            var loaded = await axGetService.LoadBaselineData(input, access);
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

    internal static AxGetService CreateService(AxpertRuntimeContext runtime)
    {
        var repository = new AxGetRepository(runtime.Common, runtime.TraceService);
        var attachmentsRepository = new AxAttachmentsRepository(runtime.Common);
        var attachmentsService = new AxAttachmentsService(
            runtime.Common,
            attachmentsRepository,
            runtime.TraceService);
        return new AxGetService(
            repository,
            runtime.Common,
            runtime.TraceService,
            attachmentsService,
            new AxParserProvider(runtime.Common, runtime.TraceService));
    }

    internal static AxTstructData.AxGet CreateInput(
        AxpertDataSourceRequest request,
        AxpertRuntimeContext runtime) =>
        new()
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
            MilliSecsInTimeStamp = runtime.Common._milliSecsInTimeStamp,
            Dimensions = runtime.Common.GetAxgApplicableDimensionsString()
        };

    internal static IReadOnlyList<AxpertDataContainerResult> ProjectContainers(object payload)
    {
        if (payload is not IDictionary containers)
        {
            throw new InvalidOperationException("AxGet returned an invalid data payload.");
        }

        var projected = new List<AxpertDataContainerResult>(containers.Count);
        foreach (DictionaryEntry container in containers)
        {
            if (container.Key is not string containerName || container.Value is not IDictionary rows)
            {
                throw new InvalidOperationException("AxGet returned an invalid data container.");
            }

            var projectedRows = new List<AxpertDataRowResult>(rows.Count);
            foreach (DictionaryEntry row in rows)
            {
                if (row.Key is not string || row.Value is not IDictionary fields)
                {
                    throw new InvalidOperationException("AxGet returned an invalid data row.");
                }

                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry field in fields)
                {
                    if (field.Key is not string fieldName || !values.TryAdd(fieldName, field.Value))
                    {
                        throw new InvalidOperationException("AxGet returned an invalid or duplicate field.");
                    }
                }

                var sourceIds = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
                long? recordId = null;
                if (values.Remove("axp_recid", out var rawRecordId) &&
                    long.TryParse(Convert.ToString(rawRecordId, CultureInfo.InvariantCulture), out var parsed))
                {
                    recordId = parsed;
                }

                foreach (var companion in values.Keys
                    .Where(name => name.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase))
                    .ToArray())
                {
                    var fieldName = companion[..^"_sourceid".Length];
                    var actualFieldName = values.Keys.SingleOrDefault(name =>
                        string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase));
                    if (actualFieldName is null || sourceIds.ContainsKey(actualFieldName))
                    {
                        throw new InvalidOperationException("AxGet returned a conflicting source-ID companion.");
                    }

                    var rawSourceId = values[companion];
                    long? sourceId = null;
                    if (rawSourceId is not null &&
                        !string.IsNullOrWhiteSpace(Convert.ToString(rawSourceId, CultureInfo.InvariantCulture)))
                    {
                        if (!long.TryParse(
                                Convert.ToString(rawSourceId, CultureInfo.InvariantCulture),
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out var parsedSourceId) ||
                            parsedSourceId < 0)
                        {
                            throw new InvalidOperationException("AxGet returned an invalid source-ID companion.");
                        }

                        sourceId = parsedSourceId == 0 ? null : parsedSourceId;
                    }

                    sourceIds.Add(actualFieldName, sourceId);
                    values.Remove(companion);
                }

                projectedRows.Add(new AxpertDataRowResult(recordId, values, sourceIds));
            }

            projected.Add(new AxpertDataContainerResult(containerName, projectedRows));
        }

        return projected;
    }

    //This examines the AxGet result and retains only structural information:
    //- Container names.
    //- Rows.
    //- Row record IDs.
    //- Which fields were returned.
    //- Which returned fields were non-null.
    //It also validates:
    //- No duplicate fields.
    //- No duplicate record ID.
    //- Source-ID companions belong to real fields.
    //- Source IDs are valid non-negative integers.
    internal static IReadOnlyList<AxpertLoadedContainerShape> ProjectLoadShape(object payload)
    {
        if (payload is not IDictionary containers)
        {
            throw new InvalidOperationException("AxGet returned an invalid data payload.");
        }

        var projected = new List<AxpertLoadedContainerShape>(containers.Count);
        foreach (DictionaryEntry container in containers)
        {
            if (container.Key is not string containerName || container.Value is not IDictionary rows)
            {
                throw new InvalidOperationException("AxGet returned an invalid data container.");
            }

            var projectedRows = new List<AxpertLoadedRowShape>(rows.Count);
            foreach (DictionaryEntry row in rows)
            {
                if (row.Key is not string || row.Value is not IDictionary fields)
                {
                    throw new InvalidOperationException("AxGet returned an invalid data row.");
                }

                long? recordId = null;
                var recordIdSeen = false;
                var returnedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var nonNullFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sourceCompanions = new List<(string Name, object? Value)>();
                foreach (DictionaryEntry field in fields)
                {
                    if (field.Key is not string fieldName)
                    {
                        throw new InvalidOperationException("AxGet returned an invalid field.");
                    }

                    if (string.Equals(fieldName, "axp_recid", StringComparison.OrdinalIgnoreCase))
                    {
                        if (recordIdSeen)
                        {
                            throw new InvalidOperationException("AxGet returned a duplicate record ID.");
                        }

                        recordIdSeen = true;
                        if (field.Value is not null &&
                            long.TryParse(
                                Convert.ToString(field.Value, CultureInfo.InvariantCulture),
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out var parsedRecordId))
                        {
                            recordId = parsedRecordId;
                        }

                        continue;
                    }

                    if (fieldName.EndsWith("_sourceid", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceCompanions.Add((fieldName, field.Value));
                        continue;
                    }

                    if (!returnedFields.Add(fieldName))
                    {
                        throw new InvalidOperationException("AxGet returned a duplicate field.");
                    }

                    if (field.Value is not null)
                    {
                        nonNullFields.Add(fieldName);
                    }
                }

                var sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var companion in sourceCompanions)
                {
                    var baseName = companion.Name[..^"_sourceid".Length];
                    var actualFieldName = returnedFields.SingleOrDefault(name =>
                        string.Equals(name, baseName, StringComparison.OrdinalIgnoreCase));
                    if (actualFieldName is null || !sourceFields.Add(actualFieldName))
                    {
                        throw new InvalidOperationException("AxGet returned a conflicting source-ID companion.");
                    }

                    if (companion.Value is not null &&
                        !string.IsNullOrWhiteSpace(Convert.ToString(companion.Value, CultureInfo.InvariantCulture)) &&
                        (!long.TryParse(
                            Convert.ToString(companion.Value, CultureInfo.InvariantCulture),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var parsedSourceId) || parsedSourceId < 0))
                    {
                        throw new InvalidOperationException("AxGet returned an invalid source-ID companion.");
                    }
                }

                projectedRows.Add(new AxpertLoadedRowShape(recordId, returnedFields, nonNullFields));
            }

            projected.Add(new AxpertLoadedContainerShape(containerName, projectedRows));
        }

        return projected;
    }

    internal static bool TryInt64(
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
