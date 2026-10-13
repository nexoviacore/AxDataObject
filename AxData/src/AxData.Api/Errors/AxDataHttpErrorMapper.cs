using AxData.Api.Contracts;
using AxData.Domain.Errors;

namespace AxData.Api.Errors;

public static class AxDataHttpErrorMapper
{
    public static IResult Map(
        AxDataErrorDefinition error,
        string correlationId,
        long? currentVersion = null)
    {
        var response = AxDataErrorResponse.Create(
            error.Code,
            error.Message,
            correlationId,
            currentVersion);

        return Results.Json(response, statusCode: error.HttpStatusCode);
    }
}
