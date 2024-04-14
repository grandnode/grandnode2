using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Themes;

public abstract class ThemeContextBase : IThemeContext
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly SecurityConfig _securityConfig;

    protected ThemeContextBase(IHttpContextAccessor contextAccessor, SecurityConfig securityConfig)
    {
        _contextAccessor = contextAccessor;
        _securityConfig = securityConfig;
    }

    public string CookiesName => $"{_securityConfig.CookiePrefix}.{AreaName}.Theme";

    public abstract string AreaName { get; }
    public abstract string GetCurrentTheme();

    public Task SetTheme(string themeName)
    {
        _contextAccessor.HttpContext?.Response.Cookies.Delete(CookiesName);
        _contextAccessor.HttpContext?.Response.Cookies.Append(CookiesName, themeName,
            new CookieOptions { HttpOnly = false, Expires = DateTimeOffset.Now.AddYears(1) });
        return Task.CompletedTask;
    }
}