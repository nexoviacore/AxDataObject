using System.Globalization;
using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Domain.Errors;
using AxData.Domain.Model;
using AxStructDef.Models;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed class AxpertFieldEngine(IAxpertFieldRuntimeFactory runtimeFactory) : IAxpertFieldEngine
{
    private static readonly SemaphoreSlim LegacyRuntimeGate = new(1, 1);

    public async Task<FieldEvaluationResult> SetValidateAndRefreshAsync(
        AxDataState state,
        FieldAddress field,
        object? value,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(user);

        if (!string.Equals(state.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(field, "The field runtime tenant does not match the trusted user tenant.");
        }

        await LegacyRuntimeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runtimeResult = await runtimeFactory.CreateAsync(
                new AxpertFieldRuntimeRequest(
                    state.TenantId,
                    state.TransId,
                    state.StructDefVersion,
                    user),
                cancellationToken).ConfigureAwait(false);

            if (!runtimeResult.Succeeded || runtimeResult.Value is null)
            {
                return Failure(field, runtimeResult.Error ?? "The Axpert field runtime could not be created.");
            }

            return await EvaluateAsync(runtimeResult.Value, state, field, value, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure(field, "Axpert field evaluation failed.");
        }
        finally
        {
            LegacyRuntimeGate.Release();
        }
    }

    private static async Task<FieldEvaluationResult> EvaluateAsync(
        AxpertFieldRuntime runtime,
        AxDataState state,
        FieldAddress address,
        object? submittedValue,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validate = runtime.Validate;
        var structDef = validate._sdef;
        var parser = validate._parser;
        if (structDef?.Flds is null || structDef.Frames is null || parser is null)
        {
            return Failure(address, "The Axpert field runtime is incomplete.");
        }

        if (!TryResolve(state, runtime, address, out var target))
        {
            return Failure(address, "The field address does not exist in the Axpert runtime.");
        }

        var recordId = state.RecordId.GetValueOrDefault();
        structDef.LastSavedRecordid = recordId;
        parser.RegisterVar("recordid", "n", recordId.ToString(CultureInfo.InvariantCulture));

        foreach (var container in state.DataContainers.Values)
        {
            if (!runtime.TryGetFrameNumber(container.Name, out var frameNumber))
            {
                return Failure(address, "A data container is not mapped by the Axpert runtime.");
            }

            for (var rowIndex = 0; rowIndex < container.Rows.Count; rowIndex++)
            {
                var row = container.Rows[rowIndex];
                var rowNumber = container.IsGrid ? rowIndex + 1 : 1;
                if (row.PersistedRecordId is long persistedRecordId &&
                    !structDef.SubmitRecordId(frameNumber, rowNumber, persistedRecordId))
                {
                    return Failure(address, "A persisted row could not be restored in the Axpert runtime.");
                }

                foreach (var pair in row.Fields)
                {
                    var legacyField = FindField(structDef.Flds, frameNumber, pair.Key);
                    if (legacyField is null)
                    {
                        return Failure(address, "A state field is not present in the Axpert StructDef.");
                    }

                    var fieldValue = ToAxpertString(pair.Value.Value);
                    structDef.SubmitValue(
                        legacyField.FieldName,
                        rowNumber,
                        fieldValue,
                        fieldValue,
                        legacyField.IDValue,
                        legacyField.OldIDValue,
                        row.PersistedRecordId ?? 0);
                    if (!legacyField.AsGrid && row.PersistedRecordId is long fieldRecordId)
                    {
                        legacyField.RecId = fieldRecordId;
                    }

                    parser.RegisterVar(legacyField.FieldName, legacyField.DataType, fieldValue);
                }
            }
        }

        var submittedText = ToAxpertString(submittedValue);
        structDef.SubmitValue(
            target.LegacyField.FieldName,
            target.RowNumber,
            submittedText,
            ToAxpertString(target.DomainField.Value),
            target.LegacyField.IDValue,
            target.LegacyField.OldIDValue,
            target.Row.PersistedRecordId ?? 0);
        parser.RegisterVar(target.LegacyField.FieldName, target.LegacyField.DataType, submittedText);

        var validation = await validate.ValidateData().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(validation.Result, "Success", StringComparison.OrdinalIgnoreCase))
        {
            return Failure(address, "Axpert rejected the submitted field value.");
        }

        var pending = new List<PendingChange>();
        foreach (var container in state.DataContainers.Values)
        {
            runtime.TryGetFrameNumber(container.Name, out var frameNumber);
            for (var rowIndex = 0; rowIndex < container.Rows.Count; rowIndex++)
            {
                var row = container.Rows[rowIndex];
                var rowNumber = container.IsGrid ? rowIndex + 1 : 1;
                foreach (var pair in row.Fields)
                {
                    var legacyField = FindField(structDef.Flds, frameNumber, pair.Key)!;
                    var evaluated = FromAxpertString(
                        structDef.GetFieldValue(legacyField, rowNumber),
                        legacyField,
                        pair.Value.Value);
                    var evaluatedAddress = new FieldAddress(container.Name, row.RowId, pair.Key);
                    var isSubmitted = AddressesEqual(evaluatedAddress, address);
                    if (isSubmitted || !ValuesEqual(pair.Value.Value, evaluated))
                    {
                        pending.Add(new PendingChange(
                            evaluatedAddress,
                            row,
                            pair.Key,
                            pair.Value,
                            evaluated,
                            isSubmitted ? FieldChangeSource.Submitted : FieldChangeSource.Dependency));
                    }
                }
            }
        }

        foreach (var change in pending)
        {
            ApplyValue(change);
        }

        return new FieldEvaluationResult(
            true,
            pending.Select(change => new FieldChange(change.Address, change.Value, change.Source)).ToArray(),
            []);
    }

    private static bool TryResolve(
        AxDataState state,
        AxpertFieldRuntime runtime,
        FieldAddress address,
        out ResolvedField resolved)
    {
        resolved = null!;
        if (!state.DataContainers.TryGetValue(address.DataContainer, out var container) ||
            !runtime.TryGetFrameNumber(container.Name, out var frameNumber))
        {
            return false;
        }

        var rowIndex = container.Rows.FindIndex(row =>
            string.Equals(row.RowId, address.RowId, StringComparison.OrdinalIgnoreCase));
        if (rowIndex < 0 ||
            !container.Rows[rowIndex].Fields.TryGetValue(address.FieldName, out var domainField))
        {
            return false;
        }

        var structDef = runtime.Validate._sdef;
        var legacyField = structDef?.Flds is null
            ? null
            : FindField(structDef.Flds, frameNumber, address.FieldName);
        if (legacyField is null)
        {
            return false;
        }

        resolved = new ResolvedField(
            container.Rows[rowIndex],
            domainField,
            legacyField,
            container.IsGrid ? rowIndex + 1 : 1);
        return true;
    }

    private static TField? FindField(IEnumerable<TField> fields, int frameNumber, string fieldName) =>
        fields.FirstOrDefault(candidate =>
            candidate.FrameNo == frameNumber &&
            string.Equals(candidate.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

    private static string ToAxpertString(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static object? FromAxpertString(string value, TField field, object? currentValue)
    {
        if (string.Equals(field.DataType, "n", StringComparison.OrdinalIgnoreCase))
        {
            return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var number)
                ? number
                : value;
        }

        if (currentValue is null && string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value;
    }

    private static void ApplyValue(PendingChange change)
    {
        if (!change.DomainField.IsReadOnly)
        {
            change.DomainField.SetValue(change.Value);
            return;
        }

        change.Row.Fields[change.FieldName] = AxFieldValue.Rehydrate(
            change.Value,
            change.DomainField.OriginalValue,
            !ValuesEqual(change.Value, change.DomainField.OriginalValue),
            true);
    }

    private static bool AddressesEqual(FieldAddress left, FieldAddress right) =>
        string.Equals(left.DataContainer, right.DataContainer, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.RowId, right.RowId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.FieldName, right.FieldName, StringComparison.OrdinalIgnoreCase);

    private static bool ValuesEqual(object? left, object? right) => Equals(left, right);

    private static FieldEvaluationResult Failure(FieldAddress address, string message) =>
        FieldEvaluationResult.Failure(
            new FieldEvaluationError(address, AxDataErrors.ValidationFailed.Code, message));

    private sealed record ResolvedField(
        AxDataRow Row,
        AxFieldValue DomainField,
        TField LegacyField,
        int RowNumber);

    private sealed record PendingChange(
        FieldAddress Address,
        AxDataRow Row,
        string FieldName,
        AxFieldValue DomainField,
        object? Value,
        FieldChangeSource Source);
}
