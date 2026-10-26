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

    public async Task<FieldOperationStartResult> CreateOperationAsync(
        AxDataState state,
        AxUserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(user);

        if (!string.Equals(state.TenantId, user.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return FieldOperationStartResult.Failure(
                "The field runtime tenant does not match the trusted user tenant.");
        }

        await LegacyRuntimeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var operation = new Operation(runtimeFactory, state, user, LegacyRuntimeGate);
            var initialized = await operation.InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (!initialized.Succeeded)
            {
                await operation.DisposeAsync().ConfigureAwait(false);
                return FieldOperationStartResult.Failure(
                    initialized.Error ?? "The Axpert field runtime could not be created.",
                    initialized.FailureKind);
            }

            return FieldOperationStartResult.Success(operation);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LegacyRuntimeGate.Release();
            throw;
        }
        catch (Exception)
        {
            LegacyRuntimeGate.Release();
            return FieldOperationStartResult.Failure("The Axpert field runtime could not be created.");
        }
    }

    private sealed class Operation(
        IAxpertFieldRuntimeFactory runtimeFactory,
        AxDataState state,
        AxUserContext user,
        SemaphoreSlim gate) : IAxpertFieldOperation
    {
        private readonly List<Submission> _accepted = [];
        private AxpertFieldRuntime? _runtime;
        private bool _contaminated;
        private bool _completed;
        private bool _disposed;

        public Task<FieldOperationStatus> InitializeAsync(CancellationToken cancellationToken) =>
            CreateAndHydrateAsync(cancellationToken);

        public async Task<FieldEvaluationResult> EvaluateAsync(
            FieldAddress field,
            object? value,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(field);
            if (_disposed || _completed || _runtime is null || _contaminated)
            {
                return Failure(field, "The Axpert field operation is not ready for evaluation.");
            }

            try
            {
                var evaluated = await EvaluateRuntimeAsync(
                    _runtime,
                    state,
                    field,
                    value,
                    cancellationToken).ConfigureAwait(false);
                if (!evaluated.Succeeded)
                {
                    _contaminated = true;
                    return evaluated;
                }

                _accepted.Add(new Submission(field, value));
                return FieldEvaluationResult.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                _contaminated = true;
                return Failure(field, "Axpert field evaluation failed.");
            }
        }

        public async Task<FieldOperationStatus> RebuildAsync(CancellationToken cancellationToken)
        {
            if (_disposed || _completed)
            {
                return FieldOperationStatus.Failure("The Axpert field operation cannot be rebuilt.");
            }

            var initialized = await CreateAndHydrateAsync(cancellationToken).ConfigureAwait(false);
            if (!initialized.Succeeded)
            {
                return initialized;
            }

            foreach (var submission in _accepted)
            {
                var replayed = await EvaluateRuntimeAsync(
                    _runtime!,
                    state,
                    submission.Address,
                    submission.Value,
                    cancellationToken).ConfigureAwait(false);
                if (!replayed.Succeeded)
                {
                    _contaminated = true;
                    return FieldOperationStatus.Failure(
                        replayed.Errors.FirstOrDefault()?.Message ?? "An accepted field could not be replayed.");
                }
            }

            _contaminated = false;
            return FieldOperationStatus.Success;
        }

        public async Task<FieldEvaluationResult> CompleteAsync(CancellationToken cancellationToken)
        {
            if (_disposed || _completed || _runtime is null)
            {
                return Failure(
                    _accepted.LastOrDefault()?.Address ?? UnknownAddress(),
                    "The Axpert field operation cannot be completed.");
            }

            if (_contaminated)
            {
                if (_accepted.Count == 0)
                {
                    _completed = true;
                    return FieldEvaluationResult.Success();
                }

                var rebuilt = await RebuildAsync(cancellationToken).ConfigureAwait(false);
                if (!rebuilt.Succeeded)
                {
                    return Failure(
                        _accepted[^1].Address,
                        rebuilt.Error ?? "The accepted Axpert field state could not be rebuilt.",
                        rebuilt.FailureKind == FieldOperationFailureKind.DefinitionChanged
                            ? AxDataErrors.StructDefChanged.Code
                            : AxDataErrors.ValidationFailed.Code);
                }
            }

            try
            {
                var result = ReadBack(_runtime, state, _accepted.Select(item => item.Address));
                _completed = result.Succeeded;
                return result;
            }
            catch (Exception)
            {
                return Failure(
                    _accepted.LastOrDefault()?.Address ?? UnknownAddress(),
                    "The Axpert field operation result could not be read.");
            }
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _runtime = null;
                gate.Release();
            }

            return ValueTask.CompletedTask;
        }

        private async Task<FieldOperationStatus> CreateAndHydrateAsync(CancellationToken cancellationToken)
        {
            var runtimeResult = await runtimeFactory.CreateAsync(
                new AxpertFieldRuntimeRequest(
                    state.TenantId,
                    state.TransId,
                    state.StructDefVersion,
                    user,
                    state.RecordId),
                cancellationToken).ConfigureAwait(false);
            if (!runtimeResult.Succeeded || runtimeResult.Value is null)
            {
                return FieldOperationStatus.Failure(
                    runtimeResult.Error ?? "The Axpert field runtime could not be created.",
                    runtimeResult.DefinitionChanged
                        ? FieldOperationFailureKind.DefinitionChanged
                        : FieldOperationFailureKind.DependencyUnavailable);
            }

            var hydrated = Hydrate(runtimeResult.Value, state, cancellationToken);
            if (!hydrated.Succeeded)
            {
                return hydrated;
            }

            _runtime = runtimeResult.Value;
            _runtime.MarkHydrated();
            _contaminated = false;
            return FieldOperationStatus.Success;
        }

        private static FieldOperationStatus Hydrate(
            AxpertFieldRuntime runtime,
            AxDataState state,
            CancellationToken cancellationToken)
        {
            var validate = runtime.Validate;
            var structDef = validate._sdef;
            var parser = validate._parser;
            if (structDef?.Flds is null || structDef.Frames is null || parser is null)
            {
                return FieldOperationStatus.Failure("The Axpert field runtime is incomplete.");
            }

            var recordId = state.RecordId.GetValueOrDefault();
            structDef.LastSavedRecordid = recordId;
            parser.RegisterVar("recordid", "n", recordId.ToString(CultureInfo.InvariantCulture));

            foreach (var container in state.DataContainers.Values)
            {
                if (!runtime.TryGetFrameNumber(container.Name, out var frameNumber))
                {
                    return FieldOperationStatus.Failure("A data container is not mapped by the Axpert runtime.");
                }

                for (var rowIndex = 0; rowIndex < container.Rows.Count; rowIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var row = container.Rows[rowIndex];
                    var rowNumber = container.IsGrid ? rowIndex + 1 : 1;
                    if (row.PersistedRecordId is long persistedRecordId &&
                        !structDef.SubmitRecordId(frameNumber, rowNumber, persistedRecordId))
                    {
                        return FieldOperationStatus.Failure(
                            "A persisted row could not be restored in the Axpert runtime.");
                    }

                    foreach (var pair in row.Fields)
                    {
                        if (!runtime.TryGetField(frameNumber, pair.Key, out var legacyField))
                        {
                            return FieldOperationStatus.Failure(
                                "A state field is not present in the Axpert StructDef.");
                        }

                        var field = pair.Value;
                        var baselineValue = field.IsDirty ? field.OriginalValue : field.Value;
                        var baselineSourceId = field.IsDirty ? field.OriginalSourceId : field.SourceId;
                        if (!structDef.SubmitValueOnLoad(
                                legacyField.FieldName,
                                rowNumber,
                                ToAxpertString(baselineValue),
                                baselineSourceId.GetValueOrDefault()))
                        {
                            return FieldOperationStatus.Failure(
                                "A field baseline could not be restored in the Axpert runtime.");
                        }

                        if (field.IsDirty)
                        {
                            structDef.SubmitValue(
                                legacyField.FieldName,
                                rowNumber,
                                ToAxpertString(field.Value),
                                ToAxpertString(field.OriginalValue),
                                field.SourceId.GetValueOrDefault(),
                                field.OriginalSourceId.GetValueOrDefault(),
                                row.PersistedRecordId.GetValueOrDefault());
                        }

                        if (!legacyField.AsGrid && row.PersistedRecordId is long fieldRecordId)
                        {
                            legacyField.RecId = fieldRecordId;
                        }

                        parser.RegisterVar(
                            legacyField.FieldName,
                            legacyField.DataType,
                            ToAxpertString(field.Value));
                    }
                }
            }

            return FieldOperationStatus.Success;
        }

        private static async Task<FieldEvaluationResult> EvaluateRuntimeAsync(
            AxpertFieldRuntime runtime,
            AxDataState state,
            FieldAddress address,
            object? submittedValue,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryResolve(state, runtime, address, out var target))
            {
                return Failure(address, "The field address does not exist in the Axpert runtime.");
            }

            var validate = runtime.Validate;
            var structDef = validate._sdef;
            var parser = validate._parser;
            if (structDef is null || parser is null)
            {
                return Failure(address, "The Axpert field runtime is incomplete.");
            }

            var submittedText = ToAxpertString(submittedValue);
            structDef.SubmitValue(
                target.LegacyField.FieldName,
                target.RowNumber,
                submittedText,
                structDef.GetOldFieldValue(target.LegacyField, target.RowNumber),
                structDef.GetIDValue(target.LegacyField, target.RowNumber),
                structDef.GetOldIDValue(target.LegacyField, target.RowNumber),
                target.Row.PersistedRecordId.GetValueOrDefault());
            parser.RegisterVar(target.LegacyField.FieldName, target.LegacyField.DataType, submittedText);

            var validation = await validate.ValidateData().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return string.Equals(validation.Result, "Success", StringComparison.OrdinalIgnoreCase)
                ? FieldEvaluationResult.Success()
                : Failure(address, "Axpert rejected the submitted field value.");
        }

        private static FieldEvaluationResult ReadBack(
            AxpertFieldRuntime runtime,
            AxDataState state,
            IEnumerable<FieldAddress> submittedAddresses)
        {
            var submitted = submittedAddresses.ToArray();
            var pending = new List<PendingChange>();
            runtime.MarkReadBack();
            var structDef = runtime.Validate._sdef;
            if (structDef?.Flds is null)
            {
                return Failure(submitted.LastOrDefault() ?? UnknownAddress(), "The Axpert field runtime is incomplete.");
            }

            foreach (var container in state.DataContainers.Values)
            {
                if (!runtime.TryGetFrameNumber(container.Name, out var frameNumber))
                {
                    return Failure(submitted.LastOrDefault() ?? UnknownAddress(), "A data container is not mapped by the Axpert runtime.");
                }

                for (var rowIndex = 0; rowIndex < container.Rows.Count; rowIndex++)
                {
                    var row = container.Rows[rowIndex];
                    var rowNumber = container.IsGrid ? rowIndex + 1 : 1;
                    foreach (var pair in row.Fields)
                    {
                        if (!runtime.TryGetField(frameNumber, pair.Key, out var legacyField))
                        {
                            return Failure(submitted.LastOrDefault() ?? UnknownAddress(), "A state field is not present in the Axpert StructDef.");
                        }

                        var evaluated = FromAxpertString(
                            structDef.GetFieldValue(legacyField, rowNumber),
                            legacyField,
                            pair.Value.Value);
                        var sourceId = NormalizeSourceId(structDef.GetIDValue(legacyField, rowNumber));
                        var address = new FieldAddress(container.Name, row.RowId, pair.Key);
                        var isSubmitted = submitted.Any(candidate => AddressesEqual(candidate, address));
                        if (isSubmitted || !ValuesEqual(pair.Value.Value, evaluated) || pair.Value.SourceId != sourceId)
                        {
                            pending.Add(new PendingChange(
                                address,
                                row,
                                pair.Key,
                                pair.Value,
                                evaluated,
                                sourceId,
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
                !container.Rows[rowIndex].Fields.TryGetValue(address.FieldName, out var domainField) ||
                !runtime.TryGetField(frameNumber, address.FieldName, out var legacyField))
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

        private static void ApplyValue(PendingChange change)
        {
            if (!change.DomainField.IsReadOnly)
            {
                change.DomainField.SetValue(change.Value, change.SourceId);
                return;
            }

            var dirty = !ValuesEqual(change.Value, change.DomainField.OriginalValue) ||
                        change.SourceId != change.DomainField.OriginalSourceId;
            change.Row.Fields[change.FieldName] = AxFieldValue.Rehydrate(
                change.Value,
                change.DomainField.OriginalValue,
                dirty,
                true,
                change.SourceId,
                change.DomainField.OriginalSourceId);
        }

        private static long? NormalizeSourceId(long value) => value > 0 ? value : null;

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
                return decimal.TryParse(
                    value,
                    NumberStyles.Number | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out var number)
                    ? number
                    : value;
            }

            return currentValue is null && string.IsNullOrEmpty(value) ? null : value;
        }

        private static bool AddressesEqual(FieldAddress left, FieldAddress right) =>
            string.Equals(left.DataContainer, right.DataContainer, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(left.RowId, right.RowId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(left.FieldName, right.FieldName, StringComparison.OrdinalIgnoreCase);

        private static bool ValuesEqual(object? left, object? right) =>
            left is byte[] leftBytes && right is byte[] rightBytes
                ? leftBytes.AsSpan().SequenceEqual(rightBytes)
                : Equals(left, right);

        private static FieldAddress UnknownAddress() => new(string.Empty, string.Empty, string.Empty);

        private static FieldEvaluationResult Failure(
            FieldAddress address,
            string message,
            string? code = null) =>
            FieldEvaluationResult.Failure(
                new FieldEvaluationError(address, code ?? AxDataErrors.ValidationFailed.Code, message));

        private sealed record Submission(FieldAddress Address, object? Value);

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
            long? SourceId,
            FieldChangeSource Source);
    }
}
