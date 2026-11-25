using AxData.Api.Contracts;
using AxData.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AxData.Api.Errors;

public static class AxDataHttpErrorMapper
{
    public static IActionResult Map(
        AxDataErrorDefinition error,
        string correlationId)
    {
        var response = AxDataErrorResponse.Create(
            error.Code,
            error.Message,
            correlationId);

        return new ObjectResult(response)
        {
            StatusCode = error.HttpStatusCode
        };
    }
}
