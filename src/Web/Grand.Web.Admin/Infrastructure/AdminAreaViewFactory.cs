using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Themes;
using Grand.Web.Common.View;

namespace Grand.Web.Admin.Infrastructure;

public class AdminAreaViewFactory : IAreaViewFactory
{
    private readonly IThemeContext _themeContext;

    public AdminAreaViewFactory(IThemeContextFactory themeContextFactory)
    {
        _themeContext = themeContextFactory.GetThemeContext(AreaName);
    }

    public string AreaName => Constants.AreaAdmin;

    public IEnumerable<string> GetViewLocations(IEnumerable<string> viewLocations)
    {
        var themeName = _themeContext?.GetCurrentTheme();

        var basicViewLocations = new[] {
            $"/Areas/{AreaName}/Views/{{1}}/{{0}}.cshtml",
            $"/Areas/{AreaName}/Views/Shared/{{0}}.cshtml"
        };
        if (string.IsNullOrWhiteSpace(themeName)) return basicViewLocations;

        var themeViewLocations = new[] {
            $"/Areas/{AreaName}/Themes/{themeName}/Views/{{1}}/{{0}}.cshtml",
            $"/Areas/{AreaName}/Themes/{themeName}/Views/Shared/{{0}}.cshtml"
        };

        return themeViewLocations.Concat(basicViewLocations);
    }
}