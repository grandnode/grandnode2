using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Themes;

public abstract class ThemeContextBase : IThemeContext
{
    private readonly IHttpContextAccessor _contextAccessor;

    protected ThemeContextBase(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public abstract string AreaName { get; }

    public string CookiesName => $"Grand.{AreaName}.Theme";
    public abstract string GetCurrentTheme();

    public Task SetTheme(string themeName)
    {
        _contextAccessor.HttpContext?.Response.Cookies.Delete(CookiesName);
        _contextAccessor.HttpContext?.Response.Cookies.Append(CookiesName, themeName,
            new CookieOptions { HttpOnly = false, Expires = DateTimeOffset.Now.AddYears(1)});
        return Task.CompletedTask;
    }
}