using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using Newtonsoft.Json;

namespace Grand.Web.Common.Themes
{
    public partial class ThemeProvider : IThemeProvider
    {
        #region Fields

        private readonly IThemeList _themeList;

        #endregion

        #region Constructors

        public ThemeProvider(IThemeList themeList)
        {
            _themeList = themeList;
        }

        #endregion

        #region Methods

        public bool ThemeConfigurationExists(string themeName)
        {
            return GetConfigurations().Any(configuration => configuration.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
        }

        public IList<ThemeConfiguration> GetConfigurations()
        {
            if(string.IsNullOrEmpty(CommonPath.Param))
                return _themeList.ThemeConfigurations;

            return _themeList.ThemeConfigurations.Where(x => string.IsNullOrEmpty(x.Directory) || x.Directory == CommonPath.Param).ToList();
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
