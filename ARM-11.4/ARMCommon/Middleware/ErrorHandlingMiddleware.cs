using ARMCommon.Model;
using Newtonsoft.Json;
using System.Net;

namespace ARMCommon.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }

            if (context.Response.HasStarted) 
                return;

            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync(context);
            }

            if (context.Response.StatusCode == (int)HttpStatusCode.NotFound)
            {
                await HandleNotFoundAsync(context);
            }

            if (context.Response.StatusCode == (int)HttpStatusCode.InternalServerError)
            {
                await HandleInternalServerErrorAsync(context);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
                return Task.CompletedTask;

            ARMResult errorResponse = new ARMResult();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            errorResponse.result.Add("statuscode", context.Response.StatusCode);
            errorResponse.result.Add("message", ex.Message);
            return context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
        }

        private static Task HandleUnauthorizedAsync(HttpContext context)
        {
            ARMResult errorResponse = new ARMResult();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            errorResponse.result.Add("statuscode", context.Response.StatusCode);
            errorResponse.result.Add("message", "Unauthorized Error");
            return context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
        }


        private static Task HandleNotFoundAsync(HttpContext context)
        {
            ARMResult errorResponse = new ARMResult();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            errorResponse.result.Add("statuscode", context.Response.StatusCode);
            errorResponse.result.Add("message", "Not Found");
            return context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
        }

        private static Task HandleInternalServerErrorAsync(HttpContext context)
        {
            ARMResult errorResponse = new ARMResult();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            errorResponse.result.Add("statuscode", context.Response.StatusCode);
            errorResponse.result.Add("message", "Internal Server Error");
            return context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
        }
    }

}
