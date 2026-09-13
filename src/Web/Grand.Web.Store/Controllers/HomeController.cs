using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseHomeControllerWithSetLanguage (ARCH-001 Phase 28).
// GetStatesByCountryId/Logout/SetLanguage live in the shared base; Index/Statistics/AccessDenied
// (real per-host views) stay here. BaseHomeControllerWithSetLanguage can't inherit any single host's
// base controller (it's shared across Admin/Store), so this subclass restates its own host's
// attribute set explicitly - same pattern as ProductController/EmailAccountController/
// PictureController.
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
