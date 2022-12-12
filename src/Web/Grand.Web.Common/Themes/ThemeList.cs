using Grand.SharedKernel.Extensions;
using Newtonsoft.Json;

namespace Grand.Web.Common.Themes
{
    public class ThemeList : IThemeList
    {
        public ThemeList()
        {
            ThemeConfigurations = new List<ThemeConfiguration>();
            if (!Directory.Exists(CommonPath.ThemePath)) return;
            foreach (var themeName in Directory.GetDirectories(CommonPath.ThemePath))
            {
                var configuration = CreateThemeConfiguration(themeName);
                if (configuration != null)
                {
                    ThemeConfigurations.Add(configuration);
                }
            }
        }

        public IList<ThemeConfiguration> ThemeConfigurations { get; }

        private static ThemeConfiguration CreateThemeConfiguration(string themePath)
        {
            var themeDirectory = new DirectoryInfo(themePath);
            var themeConfigFile = new FileInfo(Path.Combine(themeDirectory.FullName, "theme.cfg"));

            if (!themeConfigFile.Exists) return null;
            var themeConfiguration = JsonConvert.DeserializeObject<ThemeConfiguration>(File.ReadAllText(themeConfigFile.FullName));
            if (themeConfiguration == null) return null;
            themeConfiguration.Name = themeDirectory.Name;
            return themeConfiguration;
        }

    }
}
