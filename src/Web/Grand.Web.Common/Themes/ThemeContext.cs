using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Themes;

public class ThemeContext : ThemeContextBase
{
    private readonly StoreInformationSettings _storeInformationSettings;
    private readonly IHttpContextAccessor _contextAccessor;

    public ThemeContext(IHttpContextAccessor contextAccessor, StoreInformationSettings storeInformationSettings) :
        base(contextAccessor)
    {
        _storeInformationSettings = storeInformationSettings;
        _contextAccessor = contextAccessor;
    }

    public override string AreaName => "";

    public override string GetCurrentTheme()
    {
        var theme = "";
        if (_storeInformationSettings.AllowCustomerToSelectTheme)
        {
            theme = _contextAccessor.HttpContext?.Session.GetString(this.SessionName);
        }
        //default store theme
        if (string.IsNullOrEmpty(theme))
            theme = _storeInformationSettings.DefaultStoreTheme;

        return theme;
    }
}

/*
{
    /// <summary>
    /// Theme context
    /// </summary>
    public class ThemeContext : IThemeContext
    {
        private readonly IWorkContext _workContext;
        private readonly IUserFieldService _userFieldService;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly IThemeProvider _themeProvider;

        private bool _themeIsCached, _adminThemeIsCached;
        private string _cachedThemeName, _cachedAdminThemeName;

        public ThemeContext(IWorkContext workContext,
            IUserFieldService userFieldService,
            StoreInformationSettings storeInformationSettings,
            IThemeProvider themeProvider)
        {
            _workContext = workContext;
            _userFieldService = userFieldService;
            _storeInformationSettings = storeInformationSettings;
            _themeProvider = themeProvider;
        }

        /// <summary>
        /// Get current theme system name
        /// </summary>
        public string WorkingThemeName
        {
            get
            {
                if (_themeIsCached)
                    return _cachedThemeName;

                var theme = "";
                if (_storeInformationSettings.AllowCustomerToSelectTheme)
                {
                    if (_workContext.CurrentCustomer != null)
                        theme = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.WorkingThemeName, _workContext.CurrentStore.Id);
                }

                //default store theme
                if (string.IsNullOrEmpty(theme))
                    theme = _storeInformationSettings.DefaultStoreTheme;

                //ensure that theme exists
                if (!_themeProvider.ThemeConfigurationExists(theme))
                {
                    var themeInstance = _themeProvider.GetConfigurations()
                        .FirstOrDefault();
                    if (themeInstance == null)
                        throw new Exception("No theme could be loaded");
                    theme = themeInstance.Name;
                }

                //cache theme
                _cachedThemeName = theme;
                _adminThemeIsCached = true;
                return theme;
            }
        }

        /// <summary>
        /// Get current theme system name
        /// </summary>
        public string AdminAreaThemeName
        {
            get
            {

                if (_adminThemeIsCached)
                    return _cachedAdminThemeName;

                var theme = "Default";

                if (!string.IsNullOrEmpty(_workContext.CurrentStore.DefaultAdminTheme))
                    theme = _workContext.CurrentStore.DefaultAdminTheme;

                if (_storeInformationSettings.AllowToSelectAdminTheme)
                {
                    if (_workContext.CurrentCustomer != null)
                    {
                        var customerTheme = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AdminThemeName, _workContext.CurrentStore.Id);
                        if (!string.IsNullOrEmpty(customerTheme))
                            theme = customerTheme;
                    }
                }
                
                //cache theme
                _cachedAdminThemeName = theme;
                _themeIsCached = true;
                return theme;
            }
        }

        /// <summary>
        /// Set current theme system name
        /// </summary>
        /// <param name="themeName"></param>
        /// <returns></returns>
        public virtual async Task SetWorkingTheme(string themeName)
        {
            if (!_storeInformationSettings.AllowCustomerToSelectTheme)
                return;

            if (_workContext.CurrentCustomer == null)
                return;

            await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.WorkingThemeName, themeName, _workContext.CurrentStore.Id);

            //clear cache
            _themeIsCached = false;
        }
    }
}
*/