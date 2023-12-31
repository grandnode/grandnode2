using Grand.Web.Admin.Extensions;
using Grand.Web.Common.View;

namespace Grand.Web.Admin.Infrastructure;

public class AdminAreaViewFactory : IAreaViewFactory
{
    public string AreaName => Constants.AreaAdmin;
    public IEnumerable<string> GetViewLocations(IEnumerable<string> viewLocations)
    {
        var basicViewLocations = new[] {
            $"/Areas/{AreaName}/Views/{{1}}/{{0}}.cshtml",
            $"/Areas/{AreaName}/Views/Shared/{{0}}.cshtml"
        };
        return basicViewLocations;
    }

    public IEnumerable<string> GetViewLocations(string themeName)
    {
        
        var basicViewLocations = new[] {
            $"/Areas/{AreaName}/Views/{{1}}/{{0}}.cshtml",
            $"/Areas/{AreaName}/Views/Shared/{{0}}.cshtml"
        };
        if (string.IsNullOrWhiteSpace(themeName))
        {
            return basicViewLocations;
        }

        var themeViewLocations = new[] {
            $"/Areas/{AreaName}/Themes/{themeName}/Views/{{1}}/{{0}}.cshtml",
            $"/Areas/{AreaName}/Themes/{themeName}/Views/Shared/{{0}}.cshtml"
        };

        return themeViewLocations.Concat(basicViewLocations);
    }
}