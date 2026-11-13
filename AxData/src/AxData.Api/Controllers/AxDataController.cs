using AxData.Api.Contracts;
using AxData.Api.Errors;
using AxData.Api.Requests;
using AxData.Api.Routing;
using AxData.Application.Init;
using AxData.Application.Query;
using AxData.Application.Set;
using AxData.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxData.Api.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.BasePath + "/axdata")]
public sealed class AxDataController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> InitializeAsync(
        [FromBody] InitializeAxDataRequest request,
        [FromServices] InitializeAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Ok(InitializeAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, HttpContext.TraceIdentifier);
    }

    [HttpPost("{objectId}/query")]
    public async Task<IActionResult> QueryAsync(
        [FromRoute] string objectId,
        [FromBody] QueryAxDataRequest request,
        [FromServices] QueryAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        if (!request.TryToCommand(objectId, out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, HttpContext.TraceIdentifier);
        }

        return await ExecuteQueryAsync(command, handler, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("{objectId}/set")]
    public async Task<IActionResult> SetAsync(
        [FromRoute] string objectId,
        [FromBody] SetAxDataRequest request,
        [FromServices] SetAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        var expectedVersion = RequestMetadataParser.GetExpectedVersion(
            HttpContext.Request,
            request.ExpectedVersion);
        if (!request.TryToCommand(
                objectId,
                expectedVersion,
                HttpContext.TraceIdentifier,
                out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, HttpContext.TraceIdentifier);
        }

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Ok(SetAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(
                result.Error!,
                HttpContext.TraceIdentifier,
                result.CurrentVersion);
    }

    [HttpGet("{objectId}")]
    public Task<IActionResult> GetAsync(
        [FromRoute] string objectId,
        [FromServices] QueryAxDataHandler handler,
        CancellationToken cancellationToken) =>
        ExecuteQueryAsync(
            new QueryAxDataCommand(objectId, SelectAll: true, []),
            handler,
            cancellationToken);

    private async Task<IActionResult> ExecuteQueryAsync(
        QueryAxDataCommand command,
        QueryAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Ok(QueryAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, HttpContext.TraceIdentifier);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok();
    }
}
