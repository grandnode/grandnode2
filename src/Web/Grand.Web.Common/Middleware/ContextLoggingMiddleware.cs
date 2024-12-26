using Grand.Infrastructure;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Middleware;

public class ContextLoggingMiddleware
{
    private const string CustomerPropertyName = "Customer";
    private const string StorePropertyName = "Store";
    private const string CurrencyPropertyName = "Currency";
    private const string LanguagePropertyName = "Language";
    private readonly RequestDelegate _next;

    public ContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IWorkContextAccessor workContextAccessor)
    {
        var workContext = workContextAccessor.WorkContext;
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