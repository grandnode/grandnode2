﻿using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Grand.Web.Common.Middleware;

public class CultureSettingMiddleware
{
    private readonly RequestDelegate _next;

    public CultureSettingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IWorkContextAccessor workContextAccessor)
    {
        if (workContextAccessor.WorkContext.WorkingLanguage != null)
        {
            var culture = new CultureInfo(workContextAccessor.WorkContext.WorkingLanguage.LanguageCulture);

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        else
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        await _next(context);
    }
}