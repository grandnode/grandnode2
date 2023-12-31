using Grand.Domain.Stores;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Admin.Infrastructure;

public class AdminThemeContext : ThemeContextBase
{
    private readonly StoreInformationSettings _storeInformationSettings;
    private readonly IHttpContextAccessor _contextAccessor;

    public AdminThemeContext(IHttpContextAccessor contextAccessor, StoreInformationSettings storeInformationSettings) :
        base(contextAccessor)
    {
        _storeInformationSettings = storeInformationSettings;
        _contextAccessor = contextAccessor;
    }

    public override string AreaName => Constants.AreaAdmin;

    public override string GetCurrentTheme()
    {
        var theme = "";
        if (_storeInformationSettings.AllowToSelectAdminTheme)
        {
            theme = _contextAccessor.HttpContext?.Session.GetString(this.SessionName);
        }
        //default store theme
        if (string.IsNullOrEmpty(theme))
            theme = _storeInformationSettings.DefaultStoreTheme;

        return theme;
    }
}