using AxData.Application.Ports.Axpert;

namespace AxData.Integration.Axpert.StructDef;

public sealed class AxpertStructDefProvider(IAxpertStructDefSource source) : IStructDefProvider
{
    /// <summary>Loads a transaction definition and rejects any source result whose identity does not match the request.</summary>
    public async Task<StructDefHandle?> GetAsync(
        string tenantId,
        string transId,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(tenantId, transId);
        var definition = await source.LoadCurrentAsync(
            tenantId,
            transId,
            cancellationToken);

        if (definition is null ||
            !Matches(definition.TenantId, tenantId) ||
            !Matches(definition.TransId, transId))
        {
            return null;
        }

        return definition;
    }

    private static void ValidateIdentity(string tenantId, string transId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transId);
    }

    private static bool Matches(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

}
