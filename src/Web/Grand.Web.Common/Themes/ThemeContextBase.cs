using Grand.Business.Core.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Themes;

public abstract class ThemeContextBase : IThemeContext
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ICookieOptionsFactory _cookieOptionsFactory;

    protected ThemeContextBase(IHttpContextAccessor contextAccessor, ICookieOptionsFactory cookieOptionsFactory)
    {
        _contextAccessor = contextAccessor;
        _cookieOptionsFactory = cookieOptionsFactory;
    }

    public string CookiesName => $"{_cookieOptionsFactory.CookiePrefix}.{AreaName}.Theme";

    public abstract string AreaName { get; }
    public abstract string GetCurrentTheme();

    public Task SetTheme(string themeName)
    {
        _contextAccessor.HttpContext?.Response.Cookies.Delete(CookiesName);
        _contextAccessor.HttpContext?.Response.Cookies.Append(CookiesName, themeName,
            _cookieOptionsFactory.Create(DateTime.Now.AddYears(1)));

        return Task.CompletedTask;
    }
}