namespace ARMCommon.ActionFilter
{
    using ARMCommon.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    public class AppSettingsValidationFilter : ActionFilterAttribute
    {
        private readonly string[] requiredKeys;

        public AppSettingsValidationFilter(params string[] requiredKeys)
        {
            this.requiredKeys = requiredKeys;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var configuration = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));

            List<string> missingKeys = new List<string>();
            foreach (string key in requiredKeys)
            {
                // Check if the key exists in any section or in the root configuration
                if (configuration[key] == null && configuration.GetSection(key).Value == null)
                {
                    missingKeys.Add(key);
                }
            }

            if (missingKeys.Count > 0)
            {
                ARMResult result = new ARMResult();
                result.result.Add("status", false);
                result.result.Add("message", $"Required keys are missing from appsettings.json: {string.Join(",", missingKeys)}");
                context.Result = new BadRequestObjectResult(JsonConvert.SerializeObject(result));
            }
        }

    }
}
