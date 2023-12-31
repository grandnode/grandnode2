namespace Grand.Web.Common.Themes;

public interface IThemeView
{
    string AreaName { get; }
    string ThemeName { get; }
    IEnumerable<string> GetViewLocations();
}