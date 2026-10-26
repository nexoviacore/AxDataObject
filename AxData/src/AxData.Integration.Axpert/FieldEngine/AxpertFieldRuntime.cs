using AxData.Application.Ports;
using AxStructDef.Models;
using AxValidate;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed record AxpertFieldRuntimeRequest(
    string TenantId,
    string TransId,
    string PublishedDefinitionVersion,
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

    public int HydrationCount { get; private set; }

    public int ReadbackCount { get; private set; }

    public bool TryGetFrameNumber(string containerName, out int frameNumber) =>
        _containerFrames.TryGetValue(containerName, out frameNumber);

    public bool TryGetField(int frameNumber, string fieldName, out TField field) =>
        _fields.TryGetValue(FieldKey(frameNumber, fieldName), out field!);

    internal void MarkHydrated() => HydrationCount++;

    internal void MarkReadBack() => ReadbackCount++;

    private static string FieldKey(int frameNumber, string fieldName) => $"{frameNumber}\n{fieldName}";
}
