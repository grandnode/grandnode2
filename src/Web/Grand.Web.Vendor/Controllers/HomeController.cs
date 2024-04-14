using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Vendor.Controllers;

public class HomeController : BaseVendorController
{
    #region Ctor

    public HomeController(
        IWorkContext workContext,
        ILogger<HomeController> logger,
        IGrandAuthenticationService authenticationService)
    {
        _workContext = workContext;
        _logger = logger;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Fields

    private readonly IWorkContext _workContext;
    private readonly ILogger<HomeController> _logger;
    private readonly IGrandAuthenticationService _authenticationService;

    #endregion

    #region Methods

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Statistics()
    {
        return View();
    }

    public IActionResult AccessDenied(string pageUrl)
    {
        _logger.LogInformation("Access denied to user #{CurrentCustomerEmail} on {PageUrl}",
            _workContext.CurrentCustomer.Email, pageUrl);
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _authenticationService.SignOut();
        return RedirectToRoute("VendorLogin");
    }

    [AcceptVerbs("Get")]
    public async Task<IActionResult> GetStatesByCountryId(
        [FromServices] ICountryService countryService,
        [FromServices] ITranslationService translationService,
        string countryId, bool? addSelectStateItem, bool? addAsterisk)
    {
        // This action method gets called via an ajax request
        if (string.IsNullOrEmpty(countryId))
            return Json(new List<dynamic>
                { new { id = "", name = translationService.GetResource("Address.SelectState") } });

        var country = await countryService.GetCountryById(countryId);
        var states = country != null ? country.StateProvinces.ToList() : new List<StateProvince>();
        var result = (from s in states
            select new { id = s.Id, name = s.Name }).ToList();
        if (addAsterisk.HasValue && addAsterisk.Value)
        {
            //asterisk
            result.Insert(0, new { id = "", name = "*" });
        }
        else
        {
            if (country == null)
            {
                //country is not selected ("choose country" item)
                if (addSelectStateItem.HasValue && addSelectStateItem.Value)
                    result.Insert(0,
                        new { id = "", name = translationService.GetResource("Vendor.Address.SelectState") });
            }
            else
            {
                //some country is selected
                if (result.Any())
                    //country has some states
                    if (addSelectStateItem.HasValue && addSelectStateItem.Value)
                        result.Insert(0,
                            new { id = "", name = translationService.GetResource("Vendor.Address.SelectState") });
            }
        }

        return Json(result);
    }

    #endregion
}