using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Grand.Web.Common.Themes
{
    public partial class ThemeProvider : IThemeProvider
    {
        #region Fields

        private readonly IList<ThemeConfiguration> _themeConfigurations = new List<ThemeConfiguration>();
        private readonly string _basePath = string.Empty;

        #endregion

        #region Constructors

        public ThemeProvider()
        {
            _basePath = CommonPath.ThemePath;
            LoadConfigurations();
        }

        #endregion

        #region Utility

        private void LoadConfigurations()
        {
            foreach (string themeName in Directory.GetDirectories(_basePath))
            {
                var configuration = CreateThemeConfiguration(themeName);
                if (configuration != null)
                {
                    _themeConfigurations.Add(configuration);
                }
            }
        }

        private ThemeConfiguration CreateThemeConfiguration(string themePath)
        {
            var themeDirectory = new DirectoryInfo(themePath);
            var themeConfigFile = new FileInfo(Path.Combine(themeDirectory.FullName, "theme.cfg"));

            if (themeConfigFile.Exists)
            {
                var themeConfiguration = JsonConvert.DeserializeObject<ThemeConfiguration>(File.ReadAllText(themeConfigFile.FullName));
                if (themeConfiguration != null)
                {
                    themeConfiguration.Name = themeDirectory.Name;
                    return themeConfiguration;
                }
            }
            return null;
        }

        #endregion

        #region Methods

        public bool ThemeConfigurationExists(string themeName)
        {
            return GetConfigurations().Any(configuration => configuration.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
        }

        public ThemeConfiguration GetConfiguration(string themeName)
        {
            return _themeConfigurations.SingleOrDefault(x => x.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
        }

        public IList<ThemeConfiguration> GetConfigurations()
        {
            return _themeConfigurations;
        }

        public ThemeInfo GetThemeDescriptorFromText(string text)
        {
            var themeDescriptor = new ThemeInfo();
            try
            {
                var themeConfiguration = JsonConvert.DeserializeObject<ThemeConfiguration>(text);
                themeDescriptor.FriendlyName = themeConfiguration.Title;
            }
            catch { }

            return themeDescriptor;
        }

        #endregion
    }
}
