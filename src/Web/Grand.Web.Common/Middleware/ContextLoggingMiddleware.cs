using Grand.Infrastructure;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Middleware;

public class ContextLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var workContext = context.RequestServices.GetRequiredService<IWorkContext>();

        var requestTelemetry = context.Features.Get<RequestTelemetry>();
        requestTelemetry.Properties.TryAdd("Customer", workContext?.CurrentCustomer?.Email);
        requestTelemetry.Properties.TryAdd("Store", workContext?.CurrentStore?.Name);
        requestTelemetry.Properties.TryAdd("Currency", workContext?.WorkingCurrency?.Name);
        requestTelemetry.Properties.TryAdd("Language", workContext?.WorkingLanguage?.Name);

        await _next(context);
    }
}