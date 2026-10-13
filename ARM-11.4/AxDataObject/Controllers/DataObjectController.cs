using ARMCommon.ActionFilter;
using ARMCommon.Filter;
using ARMCommon.Helpers;
using ARMCommon.Interface;
using AxDataObject.Interface;
using AxDataObject.Models.Dto;
using AxDataObject.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;

namespace AxDataObject.Controllers;

/// <summary>
/// DataObject Microservice Controller
///
/// All endpoints validate session internally via Utils.ValidateSession.
/// UserName is resolved from session — never trusted from client input.
/// </summary>
[Route("api/v{version:apiVersion}")]
[ApiVersion("1")]
//[Authorize]
[ServiceFilter(typeof(ValidateSessionFilter))]
//[ServiceFilter(typeof(ApiResponseFilter))]
[ApiController]

public class DataObjectController : ControllerBase
{
    private readonly IDataObjectService _doService;
    private readonly Utils _common;
    private readonly IARMLogTraceService _traceService;

    public DataObjectController(IDataObjectService service, Utils common, IARMLogTraceService traceService)
    {
        _doService = service;
        _common = common;
        _traceService = traceService;
    }

    // ── SetValue ─────────────────────────────────────────
    /// <summary>
    /// Set a single field value. Returns all fields that changed
    /// (the field itself + any dependents recalculated by RefreshField).
    ///
    /// POST /api/dataobject/setvalue
    /// Body: { transId, fieldName, rowNo, value, idValue, ARMSessionId }
    /// </summary>
    [HttpPost("setvalue")]
    public async Task<IActionResult> SetFieldValue([FromBody] SetValueRequest req)
    {
        var isvalidSession = await ValidateSessionAsync(req.ARMSessionId);
        if (!isvalidSession.Success)
            return Unauthorized(isvalidSession.Error);

        var result = await _doService.SetFldValueAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── GetValue ─────────────────────────────────────────
    /// <summary>
    /// Get a single field value from the user's DataObject in Redis.
    ///
    /// GET /api/dataobject/getvalue?transId=&fieldName=&rowNo=&ARMSessionId=
    /// </summary>
    [HttpPost("getvalue")]
    public async Task<IActionResult> GetFieldValue([FromBody] GetValueRequest req)
    {
        var isvalidSession = await ValidateSessionAsync(req.ARMSessionId);
        if (!isvalidSession.Success)
            return Unauthorized(isvalidSession.Error);

        var result = await _doService.GetFldValueAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── SubmitValue ───────────────────────────────────────
    /// <summary>
    /// Batch-submit field values (all DCs, all rows).
    /// Validates after setting. Use on bulk load / paste.
    ///
    /// POST /api/dataobject/submitvalue
    /// Body: { transId, action, recordId, submitData: { dc1: [{fieldA: "val"}] }, ARMSessionId }
    /// </summary>
    [HttpPost("submitvalue")]
    public async Task<IActionResult> SubmitValue([FromBody] SubmitValueRequest req)
    {
        var isvalidSession = await ValidateSessionAsync(req.ARMSessionId);
        if (!isvalidSession.Success)
            return Unauthorized(isvalidSession.Error);

        var result = await _doService.SubmitValueAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── GetData (AxGet) ───────────────────────────────────
    /// <summary>
    /// Load a record from DB into the user's DataObject.
    /// Runs RefreshFieldsOnLoad. Returns full form data.
    ///
    /// POST /api/dataobject/getdata
    /// Body: { transId, keyField, keyValue, roles, dimensions, ARMSessionId }
    /// </summary>
    [HttpPost("getdata")]
    public async Task<IActionResult> GetData([FromBody] GetDataRequest req)
    {
        var isvalidSession = await ValidateSessionAsync(req.ARMSessionId);
        if (!isvalidSession.Success)
            return Unauthorized(isvalidSession.Error);

        var result = await _doService.GetDataAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── SetData (AxPut) ───────────────────────────────────
    /// <summary>
    /// Persist the user's DataObject to DB (create or edit).
    /// Reads DataObject from Redis, validates, runs SQL in a transaction.
    ///
    /// POST /api/dataobject/setdata
    /// Body: { transId, action, recordId, validateOnly, dimensions, ARMSessionId }
    ///
    /// Optional: include submitData to override/merge before saving.
    /// Preferred flow: setvalue calls first, then setdata with no submitData.
    /// </summary>
    //[HttpPost("setdata")]
    //public async Task<IActionResult> SetData([FromBody] SetDataRequest req)
    //{
    //var isvalidSession = await ValidateSessionAsync(req.ARMSessionId);
    //    if (!isvalidSession.Success)
    //        return Unauthorized(isvalidSession.Error);

    //    var result = await _doService.SetDataAsync(req);
    //    return result.Success ? Ok(result) : BadRequest(result);
    //}

    // ── Private ───────────────────────────────────────────

    private async Task<ValidateSessionResponse> ValidateSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return new ValidateSessionResponse { Success = false, Error = "ARMSessionId is required." };
        }

        var isValid = await _common.ValidateSession(sessionId);
        if (!isValid)
        {
            return new ValidateSessionResponse { Success = false, Error = "SessionId is not valid." };
            
        }

        return new ValidateSessionResponse { Success = true, Error = "" };
    }
}
