using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Events;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Infrastructure.Events;

namespace Nop.Web.Framework.Mvc.Filters;

/// <summary>
/// Global action filter that creates an Activity span for every controller action,
/// providing controller-level tracing without modifying BaseController.
/// </summary>
public class TracingActionFilter : IAsyncActionFilter
{
    private readonly IEventPublisher _eventPublisher;

    public TracingActionFilter(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

        using var activity = NopTelemetry.ActivitySource.StartActivity($"{controller}Controller.{action}");
        activity?.SetTag("mvc.controller", controller);
        activity?.SetTag("mvc.action", action);
        activity?.SetTag("url.path", context.HttpContext.Request.Path.Value);

        var executedContext = await next();

        activity?.SetTag("http.response.status_code", context.HttpContext.Response.StatusCode);

        if (controller == "Product" && action == "ProductDetails"
            && context.HttpContext.Response.StatusCode == 200
            && context.ActionArguments.TryGetValue("productId", out var idObj)
            && idObj is int productId && productId > 0)
        {
            await _eventPublisher.PublishAsync(new ProductDetailPageViewedEvent(productId));
        }

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            activity?.SetStatus(ActivityStatusCode.Error, executedContext.Exception.Message);
        }
    }
}
