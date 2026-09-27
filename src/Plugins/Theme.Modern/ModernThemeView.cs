using Grand.Web.Common.Themes;

namespace Theme.Modern;

public class ModernThemeView : IThemeView
{
    public string AreaName => "";
    public string ThemeName => "Modern";

    public ThemeInfo ThemeInfo => new("Modern", "~/Plugins/Theme.Modern/Content/theme.jpg",
        "Clean tech storefront theme with a sticky blurred header, card grid and dark mode", false);

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/Modern/{1}/{0}.cshtml",
            "/Views/Modern/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}