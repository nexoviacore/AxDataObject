using System.Security.Cryptography;

namespace AxData.Application.Ports;

public sealed class CryptographicRowIdGenerator : IRowIdGenerator
{
    public string NewRowId() => $"row_{RandomNumberGenerator.GetHexString(32).ToLowerInvariant()}";
}
