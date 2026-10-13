using ARMCommon.Interface;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgileConnect.Filter
{

    public class SessionActionFilter : ActionFilterAttribute
    {
        ILogger _logger;
        private readonly IRedisHelper _redis;
        public SessionActionFilter(ILoggerFactory loggerFactory, IRedisHelper redis)
        {

            _logger = loggerFactory.CreateLogger<SessionActionFilter>();
            _redis = redis;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // do something before the action executes
            //      _logger.LogInformation($"Action '{context.ActionDescriptor.DisplayName}' executing");
            //      var redisKey = context.HttpContext.Session.GetString("SessionId");
            //      var getSession = _redis.StringGetAsync(redisKey);
            //      if (getSession == null)
            //      {
            //          context.Result =
            //new RedirectToRouteResult(
            //    new RouteValueDictionary{{ "controller", "ARMSignIn" },
            //                                    { "action", "SignOut" }

            //                                  });
            //      }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
            _logger.LogInformation($"Action '{context.ActionDescriptor.DisplayName}' executed");
        }
    }
}

