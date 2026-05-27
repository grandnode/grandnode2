using Grand.Business.Common.Services.Localization;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

public class HomeController : BaseStoreController
{
    #region Ctor

    public HomeController(
        IContextAccessor contextAccessor,
        ILogger<HomeController> logger,
        IGrandAuthenticationService authenticationService)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
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

    public async Task<IActionResult> SetLanguage(string langid,
        [FromServices] ILanguageService languageService,
        [FromServices] ICustomerService _customerService,
        string returnUrl = "")
    {
        var language = await languageService.GetLanguageById(langid);
        if (language != null)
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.LanguageId,
                language.Id, _contextAccessor.StoreContext.CurrentStore.Id);

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.Action("Index", "Home", new { area = Constants.AreaStore });
        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            return RedirectToAction("Index", "Home", new { area = Constants.AreaStore });
        return Redirect(returnUrl);
    }

    [AcceptVerbs("Get")]
    public async Task<IActionResult> GetStatesByCountryId([FromServices] ICountryService countryService,
        [FromServices] ITranslationService translationService,
        string countryId, bool? addSelectStateItem, bool? addAsterisk)
    {
        // This action method gets called via an ajax request
        if (string.IsNullOrEmpty(countryId))
            return Json(new List<dynamic>
                { new { id = "", name = translationService.GetResource("Address.SelectState") } });

        var country = await countryService.GetCountryById(countryId);
        var states = country != null ? country.StateProvinces.ToList() : [];
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
                        new { id = "", name = translationService.GetResource("Admin.Address.SelectState") });
            }
            else
            {
                //some country is selected
                if (result.Any() && addSelectStateItem.HasValue && addSelectStateItem.Value)
                    //country has some states
                    result.Insert(0,
                        new { id = "", name = translationService.GetResource("Admin.Address.SelectState") });
            }
        }

        return Json(result);
    }
    public IActionResult AccessDenied()
    {
        _logger.LogInformation("Access denied");
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _authenticationService.SignOut();
        return RedirectToRoute("StoreLogin");
    }

    #endregion
}