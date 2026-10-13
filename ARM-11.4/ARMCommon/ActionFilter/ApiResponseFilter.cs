

using ARMCommon.Helpers;
using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;

namespace ARMCommon.ActionFilter
{

    public class ApiResponseFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var constantsResult = new Constants_Result();
            ARMResult armResult;
            if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.Value is ARMResult result)
                {
                    armResult = (ARMResult)objectResult.Value;

                    string message = "";
                    if(result.result.ContainsKey("message"))
                        message =  result.result["message"].ToString();

                    if (result.result.ContainsKey("messagetype") && result.result["messagetype"]?.ToString() == "Custom")
                    {
                        armResult.result["message"] = message;
                    }
                    else
                    {
                        if (constantsResult.result.ContainsKey(message))
                            armResult.result["message"] = constantsResult.result[message]?.ToString();
                        else
                            armResult.result["message"] = message;
                    }
                }
                else
                {
                    armResult = new ARMResult();
                    if (constantsResult.result.ContainsKey(objectResult.Value?.ToString() ?? ""))
                        armResult.result["message"] = constantsResult.result[objectResult.Value?.ToString() ?? ""].ToString();
                    else
                        armResult.result["message"] = objectResult.Value?.ToString() ?? "";
                }

                // Check the HTTP status code of the response
                if (objectResult.StatusCode == (int)HttpStatusCode.BadRequest || objectResult.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    if (!armResult.result.ContainsKey("success"))
                        armResult.result.Add("success", false);
                }
                else if (objectResult.StatusCode == (int)HttpStatusCode.OK || objectResult.StatusCode == (int)HttpStatusCode.PartialContent)
                {
                    if (!armResult.result.ContainsKey("success"))
                        armResult.result.Add("success", true);
                }

                if (armResult.result.ContainsKey("messagetype") && armResult.result["messagetype"]?.ToString() == "Custom")
                    armResult.result.Remove("messagetype");

                if (armResult.result.ContainsKey("message") && string.IsNullOrEmpty(armResult.result["message"]?.ToString()))
                    armResult.result.Remove("message");

                if (armResult.result.ContainsKey("error") && string.IsNullOrEmpty(armResult.result["error"]?.ToString()))
                    armResult.result.Remove("error");

                objectResult.Value = JsonConvert.SerializeObject(armResult, new JsonSerializerSettings
                {
                    StringEscapeHandling = StringEscapeHandling.Default
                });

            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Do nothing
        }
    }
}
