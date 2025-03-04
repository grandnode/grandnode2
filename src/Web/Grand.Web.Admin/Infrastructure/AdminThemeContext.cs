using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Stores;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Themes;

namespace Grand.Web.Admin.Infrastructure;

public class AdminThemeContext : ThemeContextBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly StoreInformationSettings _storeInformationSettings;
    private string _themeName;

    public AdminThemeContext(IHttpContextAccessor contextAccessor, ICookieOptionsFactory cookieOptionsFactory,StoreInformationSettings storeInformationSettings) : 
        base(contextAccessor, cookieOptionsFactory)
    {
        _storeInformationSettings = storeInformationSettings;
        _contextAccessor = contextAccessor;
    }

    public override string AreaName => Constants.AreaAdmin;

    public override string GetCurrentTheme()
    {
        if (!string.IsNullOrEmpty(_themeName))
            return _themeName;

        var theme = "";
        if (_storeInformationSettings.AllowToSelectAdminTheme)
            theme = _contextAccessor.HttpContext?.Request.Cookies[CookiesName];
        //default store theme
        if (string.IsNullOrEmpty(theme))
            theme = _storeInformationSettings.DefaultStoreTheme;

        return _themeName = theme;
    }
}