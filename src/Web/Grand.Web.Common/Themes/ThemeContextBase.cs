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

    public string SessionName => $"{AreaName}_Theme";
    public abstract string GetCurrentTheme();

    public Task SetTheme(string themeName)
    {
        _contextAccessor.HttpContext?.Session.SetString(SessionName, themeName);
        return Task.CompletedTask;
    }
}