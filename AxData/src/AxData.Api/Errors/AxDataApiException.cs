using AxData.Domain.Errors;

namespace AxData.Api.Errors;

public sealed class AxDataApiException(
    AxDataErrorDefinition error,
    long? currentVersion = null) : Exception(error.Message)
{
    public AxDataErrorDefinition Error { get; } = error;

    public long? CurrentVersion { get; } = currentVersion;
}
