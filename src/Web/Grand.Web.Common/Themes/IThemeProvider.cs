using Grand.Infrastructure.Plugins;
using System.Collections.Generic;

namespace Grand.Web.Common.Themes
{
    public partial interface IThemeProvider
    {
        bool ThemeConfigurationExists(string themeName);

        ThemeConfiguration GetConfiguration(string themeName);

        IList<ThemeConfiguration> GetConfigurations();

        ThemeInfo GetThemeDescriptorFromText(string text);
    }
}
