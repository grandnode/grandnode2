namespace Grand.Web.Common.View;

public class DefaultAreaViewFactory : IAreaViewFactory
{
    public string AreaName => "";
    
    private readonly IEnumerable<IThemeViewFactory> _themeFactories;

    public DefaultAreaViewFactory(IEnumerable<IThemeViewFactory> themeFactories)
    {
        _themeFactories = themeFactories.Where(x=>x.AreaName == AreaName);
    }

    public IEnumerable<string> GetViewLocations(IEnumerable<string> viewLocations)
    {
        var themeName = "";
        
        if(string.IsNullOrEmpty(themeName)) return GetDefaultViewLocations();
        
        var themeViewLocations = _themeFactories
            .Where(x=>x.ThemeName == themeName)
            .SelectMany(x=>x.GetViewLocations())
            .ToList();
        return themeViewLocations.Any() ? themeViewLocations.Concat(GetDefaultViewLocations()) : GetDefaultViewLocations();
    }

    private IEnumerable<string> GetDefaultViewLocations()
    {
        return new List<string> {
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        };
    }
}