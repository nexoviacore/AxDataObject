using AxData.Application.Ports;
using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.FillGrid;
using AxStructDef.Models;
using AxValidate;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed record AxpertFieldRuntimeRequest(
    string TenantId,
    string TransId,
    AxUserContext User,
    long? RecordId = null);

public interface IAxpertFieldRuntimeFactory
{
    Task<AxpertOperationResult<AxpertFieldRuntime>> CreateAsync(
        AxpertFieldRuntimeRequest request,
        CancellationToken cancellationToken);
}

public sealed class AxpertFieldRuntime
{
    private readonly IReadOnlyDictionary<string, int> _containerFrames;
    private readonly IReadOnlyDictionary<string, TField> _fields;

    public AxpertFieldRuntime(Validate validate, IReadOnlyDictionary<string, int> containerFrames)
    {
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(containerFrames);

        Validate = validate;
        _containerFrames = new Dictionary<string, int>(containerFrames, StringComparer.OrdinalIgnoreCase);
        _fields = (validate._sdef?.Flds ?? [])
            .ToDictionary(field => FieldKey(field.FrameNo, field.FieldName), StringComparer.OrdinalIgnoreCase);
    }

    public Validate Validate { get; }

    public StructDefHandle? Definition { get; private set; }

    public AxpertFillGridRuntime? FillGridRuntime { get; private set; }

    internal void AttachSetMetadata(
        StructDefHandle definition,
        AxpertFillGridRuntime fillGridRuntime)
    {
        Definition = definition;
        FillGridRuntime = fillGridRuntime;
    }

    public bool TryGetFrameNumber(string containerName, out int frameNumber) =>
        _containerFrames.TryGetValue(containerName, out frameNumber);

    public bool TryGetField(int frameNumber, string fieldName, out TField field) =>
        _fields.TryGetValue(FieldKey(frameNumber, fieldName), out field!);

    private static string FieldKey(int frameNumber, string fieldName) => $"{frameNumber}\n{fieldName}";
}
