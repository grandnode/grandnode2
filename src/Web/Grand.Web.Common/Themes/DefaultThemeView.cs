namespace Grand.Web.Common.Themes;

public class DefaultThemeView : IThemeView
{
    public string AreaName => "";
    public string ThemeName => "Default";

    public ThemeInfo ThemeInfo =>
        new("Default theme", "~/assets/samples/default-theme.jpg", "Default GrandNode theme", true);

    public IEnumerable<string> GetViewLocations()
    {
        return new List<string> {
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}