using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Web.Framework.Mvc.Filters;

/// <summary>
/// Global action filter that creates an Activity span for every controller action,
/// providing controller-level tracing without modifying BaseController.
/// </summary>
public class TracingActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

        using var activity = NopTelemetry.ActivitySource.StartActivity($"{controller}Controller.{action}");
        activity?.SetTag("mvc.controller", controller);
        activity?.SetTag("mvc.action", action);

        var executedContext = await next();

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            activity?.SetStatus(ActivityStatusCode.Error, executedContext.Exception.Message);
        }
    }
}
