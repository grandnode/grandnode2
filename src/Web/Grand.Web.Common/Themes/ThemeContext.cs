using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Themes;

public class ThemeContext : ThemeContextBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly StoreInformationSettings _storeInformationSettings;
    private string _themeName;

    public ThemeContext(
        IHttpContextAccessor contextAccessor,
        SecurityConfig securityConfig,
        StoreInformationSettings storeInformationSettings) :
        base(contextAccessor, securityConfig)
    {
        _storeInformationSettings = storeInformationSettings;
        _contextAccessor = contextAccessor;
    }

    public override string AreaName => "";

    public override string GetCurrentTheme()
    {
        if (!string.IsNullOrEmpty(_themeName))
            return _themeName;

        var theme = "";
        if (_storeInformationSettings.AllowCustomerToSelectTheme)
            theme = _contextAccessor.HttpContext?.Request.Cookies[CookiesName];

        //default store theme
        if (string.IsNullOrEmpty(theme))
            theme = _storeInformationSettings.DefaultStoreTheme;

        return _themeName = theme;
    }
}