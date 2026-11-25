using AxData.Api.Contracts;
using AxData.Api.Errors;
using AxData.Api.Middleware;
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
    [HttpPost("init")]
    public async Task<IActionResult> InitializeAsync(
        [FromBody] InitializeAxDataRequest request,
        [FromServices] InitializeAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.Succeeded
            ? Ok(InitializeAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, HttpContext.TraceIdentifier);
    }

    [HttpPost("get")]
    public async Task<IActionResult> GetAsync(
        [FromBody] GetAxDataRequest request,
        [FromServices] QueryAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        RequestOperationalMetadata.SetObjectId(HttpContext, request.ObjectId);
        if (!request.TryToCommand(out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, HttpContext.TraceIdentifier);
        }

        return await ExecuteQueryAsync(command, handler, cancellationToken);
    }

    [HttpPost("set")]
    public async Task<IActionResult> SetAsync(
        [FromBody] SetAxDataRequest request,
        [FromServices] SetAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        RequestOperationalMetadata.SetObjectId(HttpContext, request.ObjectId);
        if (!request.TryToCommand(HttpContext.TraceIdentifier, out var command))
        {
            return AxDataHttpErrorMapper.Map(AxDataErrors.InvalidRequest, HttpContext.TraceIdentifier);
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.Succeeded
            ? Ok(SetAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, HttpContext.TraceIdentifier);
    }

    private async Task<IActionResult> ExecuteQueryAsync(
        QueryAxDataCommand command,
        QueryAxDataHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.Succeeded
            ? Ok(GetAxDataApiResponse.From(result.Response!, HttpContext.TraceIdentifier))
            : AxDataHttpErrorMapper.Map(result.Error!, HttpContext.TraceIdentifier);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok();
    }
}
