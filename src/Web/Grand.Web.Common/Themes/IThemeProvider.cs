using Grand.Infrastructure.Plugins;

namespace Grand.Web.Common.Themes
{
    public partial interface IThemeProvider
    {
        bool ThemeConfigurationExists(string themeName);

        IList<ThemeConfiguration> GetConfigurations();

        ThemeInfo GetThemeDescriptorFromText(string text);
    }
}
