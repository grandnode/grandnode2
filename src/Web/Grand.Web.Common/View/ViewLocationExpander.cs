using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.View;

public class ViewLocationExpander : IViewLocationExpander
{
    private const string ThemeKey = "Theme";
    private const string AdminSharedFallbackLocation = "/Views/{1}/{0}.cshtml";
    private const string AdminSharedControllersNamespace = "Grand.Web.AdminShared.Controllers";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var themeContextFactory =
            context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeContextFactory>();
        var themeContext = themeContextFactory.GetThemeContext(context.AreaName ?? "");
        var themeName = themeContext?.GetCurrentTheme();
        if (!string.IsNullOrEmpty(themeName))
            context.Values[ThemeKey] = themeContext.GetCurrentTheme();
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(ThemeKey, out _))
        {
            var viewFactory = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IViewFactory>();
            viewFactory.GetViewPath(context.AreaName ?? "", ref viewLocations);
        }

        if (IsAdminSharedController(context.ActionContext.ActionDescriptor))
            viewLocations = viewLocations.Append(AdminSharedFallbackLocation);

        return viewLocations;
    }

    /// <summary>Whether the executing action's controller type (or any base type) lives in
    /// Grand.Web.AdminShared.Controllers. Generic by design — no per-entity base-controller
    /// list to maintain: the moment a future Base*Controller (Order, Category, ...) lands in
    /// that namespace, its host subclasses get the AdminShared view fallback automatically.</summary>
    internal static bool IsAdminSharedController(ActionDescriptor descriptor)
    {
        if (descriptor is not ControllerActionDescriptor cad) return false;
        for (var t = cad.ControllerTypeInfo.AsType(); t is not null; t = t.BaseType)
            if (t.Namespace == AdminSharedControllersNamespace)
                return true;
        return false;
    }
}
