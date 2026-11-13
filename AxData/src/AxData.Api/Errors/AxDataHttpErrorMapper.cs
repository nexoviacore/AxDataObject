using AxData.Api.Contracts;
using AxData.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AxData.Api.Errors;

public static class AxDataHttpErrorMapper
{
    public static IActionResult Map(
        AxDataErrorDefinition error,
        string correlationId,
        long? currentVersion = null)
    {
        var response = AxDataErrorResponse.Create(
            error.Code,
            error.Message,
            correlationId,
            currentVersion);

        return new ObjectResult(response)
        {
            StatusCode = error.HttpStatusCode
        };
    }
}
