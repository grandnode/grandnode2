using Grand.Web.Common.Themes;

namespace Grand.Web.Common.View;

public class DefaultAreaViewFactory : IAreaViewFactory
{
    public string AreaName => "";
    
    private readonly IEnumerable<IThemeView> _themeFactories;
    private readonly IThemeContext _themeContext;
    
    public DefaultAreaViewFactory(IEnumerable<IThemeView> themeFactories, IThemeContextFactory themeContextFactory)
    {
        _themeContext = themeContextFactory.GetThemeContext(AreaName);
        _themeFactories = themeFactories.Where(x=>x.AreaName == AreaName);
    }

    public IEnumerable<string> GetViewLocations(IEnumerable<string> viewLocations)
    {
        var themeName = _themeContext?.GetCurrentTheme();
        
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