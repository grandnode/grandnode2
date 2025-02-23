using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

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

    public async Task InvokeAsync(HttpContext context, IContextAccessor contextAccessor)
    {
        var workContext = contextAccessor.WorkContext;
        var storeContext = contextAccessor.StoreContext;
        Activity activity = Activity.Current;
        if (activity != null)
        {
            activity.AddTag(CustomerPropertyName, workContext?.CurrentCustomer?.Email);
            activity.AddTag(StorePropertyName, storeContext?.CurrentStore?.Name);
            activity.AddTag(CurrencyPropertyName, workContext?.WorkingCurrency?.Name);
            activity.AddTag(LanguagePropertyName, workContext?.WorkingLanguage?.Name);
        }
        await _next(context);
    }
}