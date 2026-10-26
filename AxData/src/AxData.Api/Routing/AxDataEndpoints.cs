using AxData.Api.Contracts;
using AxData.Api.Errors;
using AxData.Application.Init;
using AxData.Application.Query;
using AxData.Application.Set;
using AxData.Domain.Errors;
using AxData.Api.Requests;

namespace AxData.Api.Routing;

public static class AxDataEndpoints
{
    public static RouteGroupBuilder MapAxDataEndpoints(this RouteGroupBuilder routes)
    {
        routes.RequireAuthorization();
        routes.MapPost("/axdata", HandleInitializeAsync);
        routes.MapPost("/axdata/{objectId}/query", HandleQueryAsync);
        routes.MapPost("/axdata/{objectId}/set", HandleSetAsync);
        routes.MapGet("/axdata/{objectId}", HandleGetAsync);
        return routes;
    }

    public static async Task<IResult> HandleSetAsync(
        string objectId,
        SetAxDataRequest request,
        SetAxDataHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var expectedVersion = RequestMetadataParser.GetExpectedVersion(
            httpContext.Request,
            request.ExpectedVersion);
        if (!request.TryToCommand(objectId, expectedVersion, httpContext.TraceIdentifier, out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, httpContext.TraceIdentifier);
        }

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Results.Ok(SetAxDataApiResponse.From(result.Response!, httpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, httpContext.TraceIdentifier, result.CurrentVersion);
    }

    public static async Task<IResult> HandleInitializeAsync(
        InitializeAxDataRequest request,
        InitializeAxDataHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Results.Ok(InitializeAxDataApiResponse.From(result.Response!, httpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, httpContext.TraceIdentifier);
    }

    public static async Task<IResult> HandleQueryAsync(
        string objectId,
        QueryAxDataRequest request,
        QueryAxDataHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!request.TryToCommand(objectId, out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, httpContext.TraceIdentifier);
        }

        return await ExecuteQueryAsync(command, handler, httpContext, cancellationToken).ConfigureAwait(false);
    }

    public static Task<IResult> HandleGetAsync(
        string objectId,
        QueryAxDataHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ExecuteQueryAsync(
            new QueryAxDataCommand(objectId, SelectAll: true, []),
            handler,
            httpContext,
            cancellationToken);

    private static async Task<IResult> ExecuteQueryAsync(
        QueryAxDataCommand command,
        QueryAxDataHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Results.Ok(QueryAxDataApiResponse.From(result.Response!, httpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, httpContext.TraceIdentifier);
    }
}
