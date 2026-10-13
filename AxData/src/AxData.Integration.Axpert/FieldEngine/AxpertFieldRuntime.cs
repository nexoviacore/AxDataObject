using AxData.Application.Ports;
using AxValidate;

namespace AxData.Integration.Axpert.FieldEngine;

public sealed record AxpertFieldRuntimeRequest(
    string TenantId,
    string TransId,
    string PublishedDefinitionVersion,
    AxUserContext User);

public interface IAxpertFieldRuntimeFactory
{
    Task<AxpertOperationResult<AxpertFieldRuntime>> CreateAsync(
        AxpertFieldRuntimeRequest request,
        CancellationToken cancellationToken);
}

public sealed class AxpertFieldRuntime
{
    private readonly IReadOnlyDictionary<string, int> _containerFrames;

    public AxpertFieldRuntime(Validate validate, IReadOnlyDictionary<string, int> containerFrames)
    {
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(containerFrames);

        Validate = validate;
        _containerFrames = new Dictionary<string, int>(containerFrames, StringComparer.OrdinalIgnoreCase);
    }

    public Validate Validate { get; }

    public bool TryGetFrameNumber(string containerName, out int frameNumber) =>
        _containerFrames.TryGetValue(containerName, out frameNumber);
}
