using Grand.Web.Common.Themes;

namespace Theme.Minimal;

public class MinimalThemeView: IThemeView
{
    public string AreaName => "";
    public string ThemeName => "Minimal";
    
    public ThemeInfo ThemeInfo => new ("Minimal theme", "~/assets/samples/default-theme.jpg", "Minimal theme", true, "1.0");

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/Minimal/{1}/{0}.cshtml",
            "/Views/Minimal/Shared/{0}.cshtml"
        };
    }
}