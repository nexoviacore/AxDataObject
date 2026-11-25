using AxData.Domain.Errors;

namespace AxData.Api.Errors;

public sealed class AxDataApiException(AxDataErrorDefinition error) : Exception(error.Message)
{
    public AxDataErrorDefinition Error { get; } = error;
}
