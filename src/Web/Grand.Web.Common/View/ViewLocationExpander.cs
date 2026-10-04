using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.View;

public class ViewLocationExpander : IViewLocationExpander
{
    private const string ThemeKey = "Theme";
    // Nested under /Views/AdminShared/ deliberately: a Razor view's compiled path is a global lookup
    // key across every ApplicationPart, and the shallow /Views/{1}/{0}.cshtml form collided with the
    // combined Grand.Web host's own storefront views (/Views/_ViewStart.cshtml,
    // /Views/Product/Partials/ProductAttributes.cshtml). The AdminShared segment is unowned by any
    // other project, so shared entity folders added in later phases cannot collide.
    private const string AdminSharedFallbackLocation = "/Views/AdminShared/{1}/{0}.cshtml";
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
            viewLocations = WithAdminSharedLocation(viewLocations);

        return viewLocations;
    }

    /// <summary>Puts the AdminShared location right after the area locations, ahead of the root
    /// /Views/{1}/{0} and /Views/Shared/{0} ones. Appended last, a partial such as
    /// "Partials/CreateOrUpdateAddress" resolved to the storefront's /Views/Shared copy (another
    /// model type) before the AdminShared one was ever tried. Without area locations the list
    /// only gets the location appended.</summary>
    internal static IEnumerable<string> WithAdminSharedLocation(IEnumerable<string> viewLocations)
    {
        var locations = viewLocations.ToList();
        var index = locations.FindIndex(l => !l.StartsWith("/Areas/", StringComparison.OrdinalIgnoreCase));
        if (index <= 0)
            index = locations.Count;
        locations.Insert(index, AdminSharedFallbackLocation);
        return locations;
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
