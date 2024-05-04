namespace Grand.Web.Common.Themes;

public class ThemeContextFactory : IThemeContextFactory
{
    private readonly IDictionary<string, IThemeContext> _themeContexts;

    public ThemeContextFactory(IEnumerable<IThemeContext> themeContexts)
    {
        _themeContexts = themeContexts.ToDictionary(f => f.AreaName, f => f);
    }

    public IThemeContext GetThemeContext(string areaName)
    {
        return _themeContexts.TryGetValue(areaName, out var themeContext) ? themeContext : null;
    }
}