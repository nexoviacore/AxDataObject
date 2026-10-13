using ARMCommon.ActionFilter;
using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/v{version:apiVersion}")]
[ApiVersion("1")]
[ApiController]
public class ARMServiceLogController : Controller
{
    private readonly DataContext context;

    public ARMServiceLogController(DataContext _context)
    {
        context = _context;
    }


    public IActionResult ARMServiceList()
    {
        var ServiceList = context.ARMServiceLogs.ToList();
        return View(ServiceList);
    }


    [RequiredFieldsFilter("ServiceName", "Status", "LastOnline", "Server", "Folder", "OtherInfo", "InstanceID")]
    [ServiceFilter(typeof(ApiResponseFilter))]
    [HttpPost("ARMCreateLog")]
    public async Task<IActionResult> ARMCreateLog(ARMServiceLogs log)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var matchingRecord = await context.ARMServiceLogs.FirstOrDefaultAsync(r => r.ServiceName == log.ServiceName.ToLower() && r.Server == log.Server.ToLower() && r.Folder == log.Folder.ToLower());
                if (matchingRecord != null)
                {
                    matchingRecord.ServiceName = log.ServiceName;
                    matchingRecord.Status = log.Status;
                    matchingRecord.LastOnline = log.LastOnline;
                    matchingRecord.Server = log.Server;
                    matchingRecord.Folder = log.Folder;
                    // matchingRecord.OtherInfo = log.OtherInfo;
                    matchingRecord.InstanceID = log.InstanceID;
                }
                else
                {
                    var newRecord = new ARMServiceLogs
                    {
                        ServiceName = log.ServiceName,
                        Status = log.Status,
                        Server = log.Server,
                        Folder = log.Folder,
                        // OtherInfo = log.OtherInfo,
                        InstanceID = log.InstanceID,
                        LastOnline = log.LastOnline,
                    };
                    context.ARMServiceLogs.Add(newRecord);
                }

                await context.SaveChangesAsync();
                return Ok(matchingRecord != null ? "DATAUPDATED" : "DATAINSERTED");
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred: {ex.Message}");
        }
    }

}

