using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Reduced to a thin subclass of BaseHomeController (ARCH-001 Phase 28). GetStatesByCountryId/Logout
// live in the shared base; Index/Statistics/AccessDenied (real per-host views) stay here. Vendor
// never had a SetLanguage action, so it extends the plain BaseHomeController rather than
// BaseHomeControllerWithSetLanguage (see that class's remarks) - inheriting the with-SetLanguage
// base would silently add a route that never existed on Vendor before. BaseHomeController can't
// inherit any single host's base controller (it's shared across Admin/Store/Vendor), so this
// subclass restates its own host's attribute set explicitly - same pattern as
// ProductController/EmailAccountController/PictureController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class HomeController : BaseHomeController
{
    #region Ctor

    public HomeController(
        ICountryService countryService,
        ITranslationService translationService,
        IGrandAuthenticationService authenticationService,
        ILogger<HomeController> logger)
        : base(countryService, translationService, authenticationService)
    {
        _logger = logger;
    }

    #endregion

    #region Fields

    private readonly ILogger<HomeController> _logger;

    #endregion

    #region Methods

    protected override string SelectStateResourceKey => "Vendor.Address.SelectState";
    protected override string LogoutRouteName => "VendorLogin";

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
