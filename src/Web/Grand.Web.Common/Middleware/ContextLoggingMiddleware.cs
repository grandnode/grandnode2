using Grand.Infrastructure;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Middleware;

public class ContextLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private const string CustomerPropertyName = "Customer";
    private const string StorePropertyName = "Store";
    private const string CurrencyPropertyName = "Currency";
    private const string LanguagePropertyName = "Language";
    public ContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var workContext = context.RequestServices.GetRequiredService<IWorkContext>();

        var requestTelemetry = context.Features.Get<RequestTelemetry>();
        if (requestTelemetry != null)
        {
            requestTelemetry.Properties.TryAdd(CustomerPropertyName, workContext?.CurrentCustomer?.Email);
            requestTelemetry.Properties.TryAdd(StorePropertyName, workContext?.CurrentStore?.Name);
            requestTelemetry.Properties.TryAdd(CurrencyPropertyName, workContext?.WorkingCurrency?.Name);
            requestTelemetry.Properties.TryAdd(LanguagePropertyName, workContext?.WorkingLanguage?.Name);
        }

        await _next(context);
    }
}