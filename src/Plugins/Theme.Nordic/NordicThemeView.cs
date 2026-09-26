using Grand.Web.Common.Themes;

namespace Theme.Nordic;

public class NordicThemeView : IThemeView
{
    public string AreaName => "";
    public string ThemeName => "Nordic";

    public ThemeInfo ThemeInfo => new("Nordic Editorial", "~/Plugins/Theme.Nordic/Content/theme.jpg",
        "Editorial storefront theme with serif display type, full-bleed imagery and dark mode", false);

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/Nordic/{1}/{0}.cshtml",
            "/Views/Nordic/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}
