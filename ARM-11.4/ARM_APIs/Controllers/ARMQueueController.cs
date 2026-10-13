using Microsoft.AspNetCore.Mvc;
using ARMCommon.Model;
using Newtonsoft.Json;
using ARMCommon.ActionFilter;
using ARM_APIs.Model;
using ARM_APIs.Services;

namespace ARM_APIs.Controllers
{
    [Route("api/v{version:apiVersion}")]
    [ApiVersion("1")]
    [ApiController]
    public class ARMQueueController : Controller
    {
      
      private readonly IARMPushToQueue _queueService;

        public ARMQueueController(IARMPushToQueue queueService)
        {
          _queueService = queueService;
        }

        [RequiredFieldsFilter("SecretKey", "QueueName", "SubmitData")]
        [HttpPost("ARMQueueSubmit")]
        public async Task<IActionResult> ARMQueueSubmit(ARMInboundQueue queueData)
        {
            ARMResult result = new ARMResult();
            SQLResult queueDetails = await _queueService.GetInboundQueue(queueData.Project, queueData.QueueName);
            if (queueDetails != null)
            {
                if (!string.IsNullOrEmpty(queueDetails.error))
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. {queueDetails.error}");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else if (queueDetails.data != null && queueDetails.data.Rows.Count == 0)
                {
                    result.result.Add("success", false);
                    result.result.Add("message", $"Error. Invalid InboundQueue details.");
                    return BadRequest(JsonConvert.SerializeObject(result.result));
                }
                else
                {
                    ARMInboundQueue queueObj = new ARMInboundQueue();
                    queueObj = await _queueService.GetQueueObject(queueDetails);

                    string queueResult = await _queueService.SendToQueue(queueData, queueObj);
                    if (queueResult.ToLower() == "true")
                    {
                        result.result.Add("success", true);
                        result.result.Add("message", "Added to Queue");
                        return Ok(JsonConvert.SerializeObject(result.result));
                    }
                    else
                    {
                        result.result.Add("success", false);
                        result.result.Add("message", $"Error. {queueResult}");
                        return BadRequest(JsonConvert.SerializeObject(result.result));
                    }
                }
            }
            else
            {
                result.result.Add("success", false);
                result.result.Add("message", "Error. Invalid InboundQueue details.");
                return BadRequest(JsonConvert.SerializeObject(result.result));
            }
        }


        [RequiredFieldsFilter("queuename","queuedata")]
        [ServiceFilter(typeof(ApiResponseFilter))]
        [HttpPost("ARMPushToQueue")]
        public IActionResult ARMPushToQueue(ARMQueueData queueData)
        {
            if (_queueService.PushToQueue(queueData))
            {
                return Ok("QUEUEDATAINSERTEDSUCESSFULLY");
            }
            else
            {
                return BadRequest("QUEUEDATAINSERTIONFAILED");
            }
        }
    

}
}
