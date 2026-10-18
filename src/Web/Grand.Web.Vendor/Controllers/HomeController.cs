using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Vendor.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class HomeController : BaseHomeController
{
    #region Ctor

    public HomeController(
        ICountryService countryService,
        IRequestTranslationService translationService,
        ILogger<HomeController> logger)
        : base(countryService, translationService)
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
