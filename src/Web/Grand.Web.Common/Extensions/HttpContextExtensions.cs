using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Extensions;

public static class HttpContextExtensions
{
    public static string GetStoreCookie(this HttpContext httpContext)
    {
        return httpContext?.Request.Cookies[CommonHelper.StoreCookieName];
    }
}