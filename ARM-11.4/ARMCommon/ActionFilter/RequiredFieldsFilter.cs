using ARMCommon.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ARMCommon.ActionFilter
{
    public class RequiredFieldsFilter : ActionFilterAttribute
    {
        private readonly string[] _fields;

        public RequiredFieldsFilter(params string[] fields)
        {
            _fields = fields;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
           var type = context.ActionArguments.First().Value.GetType();
            var missingFields = new List<string>();

            foreach (var field in _fields)
            {
                var property = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null || string.IsNullOrEmpty(property.GetValue(context.ActionArguments.First().Value)?.ToString()))
                {
                    missingFields.Add(field);
                }
            }

            if (missingFields.Count > 0)
            {
                ARMResult result = new ARMResult();
                result.result.Add("status", false);
                result.result.Add("messagetype", "Custom");
                if(missingFields.Count == 1)
                    result.result.Add("message", "Required field : '" + missingFields[0] + "' cannot be empty.");
                else
                    result.result.Add("message", "Required field(s) : '" + string.Join(", ", missingFields) + "' cannot be empty.");

                context.Result = new BadRequestObjectResult(result);
            }
        }
    }

    public class RequiredFieldsFilterAxpertResult : ActionFilterAttribute
    {
        private readonly string[] _fields;

        public RequiredFieldsFilterAxpertResult(params string[] fields)
        {
            _fields = fields;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var type = context.ActionArguments.First().Value.GetType();
            var missingFields = new List<string>();

            foreach (var field in _fields)
            {
                var property = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property == null || string.IsNullOrEmpty(property.GetValue(context.ActionArguments.First().Value)?.ToString()))
                {
                    missingFields.Add(field);
                }
            }

            if (missingFields.Count > 0)
            {
                ARMResult result = new ARMResult();
                result.result.Add("status", false);
                result.result.Add("messagetype", "Custom");
                if (missingFields.Count == 0)
                    result.result.Add("message", "Required field : '" + missingFields + "' cannot be empty.");
                else
                    result.result.Add("message", "Required field(s) : '" + string.Join(", ", missingFields) + "' cannot be empty.");
                context.Result = new BadRequestObjectResult(result);
            }
        }
    }

    //public class TraceAttribute : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext context)
    //    {
    //        // Enable buffering to allow multiple reads of the request body
    //        context.HttpContext.Request.EnableBuffering();

    //        context.HttpContext.Request.Body.Position = 0;

    //        string requestBody = string.Empty;

    //        // Read the request body
    //        using (var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
    //        {
    //            requestBody = reader.ReadToEnd();
    //        }

    //        // Reset position for further processing
    //        context.HttpContext.Request.Body.Position = 0;

    //        // Log headers
    //        foreach (var header in context.HttpContext.Request.Headers)
    //        {
    //            Console.WriteLine($"{header.Key}: {header.Value}");
    //        }

    //        // Extract "Trace" value from JSON request body
    //        if (!string.IsNullOrEmpty(requestBody) &&
    //            context.HttpContext.Request.ContentType?.Contains("application/json") == true)
    //        {
    //            try
    //            {
    //                JObject json = JObject.Parse(requestBody);
    //                bool trace = json.Value<bool?>("Trace") ?? false;

    //                // Store "trace" in HttpContext.Items
    //                context.HttpContext.Items["trace"] = trace;
    //            }
    //            catch (JsonException ex)
    //            {
    //                Console.WriteLine($"Error parsing JSON: {ex.Message}");
    //            }
    //        }

    //        base.OnActionExecuting(context);
    //    }

    //    public override void OnActionExecuted(ActionExecutedContext context)
    //    {
    //        // Retrieve "trace" value from HttpContext.Items and convert it to a string
    //        // Retrieve the Trace value from HttpContext.Items
    //        bool trace = context.HttpContext.Items.ContainsKey("Trace") &&
    //                     (bool)context.HttpContext.Items["Trace"];


    //        Console.WriteLine($"Trace Value in OnActionExecuted: {trace}");

    //        // Modify the result if it's of type ARMResult
    //        if (context.Result is ObjectResult result && result.Value is ARMResult armResult)
    //        {
    //            armResult.result.Add("trace", trace);

    //            if (trace)
    //            {
    //                string logMessage = $"Trace: {trace}\nRequest Path: {context.HttpContext.Request.Path}";
    //                WriteLogToFile(context).GetAwaiter().GetResult(); // Await the async method
    //            }

    //            result.Value = armResult;
    //        }

    //        base.OnActionExecuted(context);
    //    }



    //    public async Task WriteLogToFile(ActionExecutedContext context)
    //    {
    //        try
    //        {
    //            // Get the request JSON from the body if available
    //            string requestJson = await GetRequestBodyAsync(context.HttpContext);

    //            // Build the log entry with timestamp, request details, and response details
    //            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
    //                              $"Request Path: {context.HttpContext.Request.Path}\n" +
    //                              $"Trace: true\n" +
    //                              $"Request JSON: {requestJson}\n" +
    //                              $"Response: {JsonConvert.SerializeObject(context.Result)}\n" +
    //                              "----------------------------------------\n";

    //            // Write log entry to file
    //            await LogToFile(logEntry);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error while logging: {ex.Message}");
    //        }
    //    }

    //    private async Task<string> GetRequestBodyAsync(HttpContext context)
    //    {
    //        try
    //        {
    //            // Enable buffering to read the request body
    //            context.Request.EnableBuffering();

    //            // Read the request body as a string
    //            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
    //            {
    //                string requestBody = await reader.ReadToEndAsync();

    //                // Reset the request body stream position to 0 so it can be read by the controller
    //                context.Request.Body.Position = 0;

    //                return requestBody;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error reading request body: {ex.Message}");
    //            return string.Empty;
    //        }
    //    }

    //    private async Task LogToFile(string logEntry)
    //    {
    //        try
    //        {
    //            // Get the path of the bin directory dynamically
    //            string binPath = AppDomain.CurrentDomain.BaseDirectory;
    //            string logDirectory = Path.Combine(binPath, "Logs");
    //            string logFilePath = Path.Combine(logDirectory, "TraceLog.txt");

    //            // Ensure the Logs directory exists
    //            if (!Directory.Exists(logDirectory))
    //            {
    //                Directory.CreateDirectory(logDirectory);
    //            }

    //            // Write new log entry (overwriting the file content)
    //            await File.WriteAllTextAsync(logFilePath, logEntry);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error creating or writing to TraceLog.txt: {ex.Message}");
    //        }
    //    }




    //}



    //public class TraceAttribute : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext context)
    //    {
    //        // Enable buffering of the request body
    //        context.HttpContext.Request.EnableBuffering();

    //        // Rewind the stream before reading (to avoid reading it prematurely)
    //        context.HttpContext.Request.Body.Position = 0;

    //        var requestBody = string.Empty;

    //        // Read the request body
    //        using (var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
    //        {
    //            requestBody = reader.ReadToEnd();
    //        }

    //        // Rewind the body for further use (after it's read, reset the position)
    //        context.HttpContext.Request.Body.Position = 0;

    //        foreach (var header in context.HttpContext.Request.Headers)
    //        {
    //            Console.WriteLine($"{header.Key}: {header.Value}");
    //        }

    //        Console.WriteLine($"Request Body: {requestBody}");

    //        // If the body is valid, try parsing JSON
    //        if (!string.IsNullOrEmpty(requestBody) && context.HttpContext.Request.ContentType?.Contains("application/json") == true)
    //        {
    //            try
    //            {
    //                JObject json = JObject.Parse(requestBody);
    //                bool trace = json.Value<bool>("Trace");
    //                Console.WriteLine($"Extracted Trace Value: {trace}");
    //                context.HttpContext.Items["Trace"] = trace;
    //            }
    //            catch (JsonException ex)
    //            {
    //                Console.WriteLine($"Error parsing JSON: {ex.Message}");
    //            }
    //        }

    //        base.OnActionExecuting(context);
    //    }


    //    public override void OnActionExecuted(ActionExecutedContext context)
    //    {
    //        // Retrieve the Trace value from HttpContext.Items
    //        bool trace = context.HttpContext.Items.ContainsKey("Trace") &&
    //                     (bool)context.HttpContext.Items["Trace"];

    //        // Log the Trace value being applied to the response
    //        Console.WriteLine($"Trace Value in OnActionExecuted: {trace}");

    //        if (context.Result is ObjectResult result && result.Value is ARMResult armResult)
    //        {
    //            armResult.result.Add("Trace", trace);

    //            if (trace)
    //            {
    //                WriteLogToFile(context);
    //            }

    //            result.Value = armResult;
    //        }

    //        base.OnActionExecuted(context);
    //    }





    //    public void WriteLogToFile(ActionExecutedContext context)
    //    {
    //        // Existing implementation for context-based logging
    //        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
    //                          $"Request Path: {context.HttpContext.Request.Path}\n" +

    //                          $"Trace: true\n" +
    //                          $"Response: {JsonConvert.SerializeObject(context.Result)}\n" +
    //                          "----------------------------------------\n";
    //        LogToFile(logEntry);
    //    }

    //    public void WriteLogToFile(string logMessage)
    //    {
    //        // Wrapper for string-based logging
    //        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
    //                          $"{logMessage}\n" +
    //                          "----------------------------------------\n";
    //        LogToFile(logEntry);
    //    }

    //    private async Task LogToFile(string logEntry)
    //    {


    //        string binPath = AppDomain.CurrentDomain.BaseDirectory;
    //        string logDirectory = Path.Combine(binPath, "Logs");
    //        string logFilePath = Path.Combine(logDirectory, "TraceLog.txt");


    //        if (!Directory.Exists(logDirectory))
    //        {
    //            Directory.CreateDirectory(logDirectory);
    //        }

    //        await File.AppendAllTextAsync(logFilePath, logEntry);
    //    }



    //}







}
