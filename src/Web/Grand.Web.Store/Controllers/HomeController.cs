using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class HomeController : BaseHomeControllerWithSetLanguage
{
    #region Ctor

    public HomeController(
        ICountryService countryService,
        ITranslationService translationService,
        IGrandAuthenticationService authenticationService,
        IContextAccessor contextAccessor,
        ILogger<HomeController> logger)
        : base(countryService, translationService, authenticationService, contextAccessor)
    {
        _logger = logger;
    }

    #endregion

    #region Fields

    private readonly ILogger<HomeController> _logger;

    #endregion

    #region Methods

    protected override string SelectStateResourceKey => "Admin.Address.SelectState";
    protected override string LogoutRouteName => "StoreLogin";
    protected override string AreaName => Constants.AreaStore;

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Statistics()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        _logger.LogInformation("Access denied");
        return View();
    }

    #endregion
}
