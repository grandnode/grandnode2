using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.View;

public class ViewLocationExpander : IViewLocationExpander
{
    private const string ThemeKey = "Theme";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var themeContextFactory =
            context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeContextFactory>();
        var themeContext = themeContextFactory.GetThemeContext(context.AreaName ?? "");
        var themeName = themeContext?.GetCurrentTheme();
        if (!string.IsNullOrEmpty(themeName))
            context.Values[ThemeKey] = themeContext?.GetCurrentTheme();
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (!context.Values.TryGetValue(ThemeKey, out var _)) return viewLocations;

        var viewFactory = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IViewFactory>();
        viewFactory.GetViewPath(context.AreaName ?? "", ref viewLocations);

        return viewLocations;
    }
}