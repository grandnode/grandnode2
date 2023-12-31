namespace Grand.Web.Common.View;

public interface  IThemeViewFactory
{
    string AreaName { get; }
    string ThemeName { get; }
    IEnumerable<string> GetViewLocations();
}