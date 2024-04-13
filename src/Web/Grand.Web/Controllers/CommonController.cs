using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Themes;
using Grand.Web.Events;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Grand.Web.Controllers;

public class CommonController : BasePublicController
{
    #region Constructors

    public CommonController(IWorkContext workContext,
        ILanguageService languageService,
        IMediator mediator)
    {
        _workContext = workContext;
        _languageService = languageService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly ILanguageService _languageService;
    private readonly IWorkContext _workContext;
    private readonly IMediator _mediator;

    #endregion

    #region Utilities

    private string RemoveLanguageSeoCode(string url, PathString pathBase)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        _ = new PathString(url).StartsWithSegments(pathBase, out var resultpath);
        url = WebUtility.UrlDecode(resultpath);

        url = url.TrimStart('/');
        var result = url.Contains('/') ? url[url.IndexOf('/')..] : string.Empty;

        result = pathBase + result;
        return result;
    }

    private async Task<bool> IsLocalized(string url, PathString pathBase)
    {
        _ = new PathString(url).StartsWithSegments(pathBase, out var result);
        url = WebUtility.UrlDecode(result);

        var firstSegment = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
                           string.Empty;
        if (string.IsNullOrEmpty(firstSegment))
            return false;

        //suppose that the first segment is the language code and try to get language
        var language = (await _languageService.GetAllLanguages())
            .FirstOrDefault(urlLanguage =>
                urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

        return language?.Published ?? false;
    }

    private static string AddLanguageSeo(string url, Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (!string.IsNullOrEmpty(url)) url = Flurl.Url.EncodeIllegalCharacters(url);

        //add language code
        return $"/{language.UniqueSeoCode}/{url?.TrimStart('/')}";
    }

    #endregion

    #region Methods

    //page not found
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult PageNotFound()
    {
        Response.StatusCode = 404;
        Response.ContentType = "text/html";
        return View();
    }

    //access denied
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        Response.ContentType = "text/html";
        return View();
    }

    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult Route(string routeName)
    {
        if (string.IsNullOrEmpty(routeName))
            return Json(new { redirectToUrl = string.Empty });

        var url = Url.RouteUrl(routeName);

        return Json(new { redirectToUrl = url });
    }

    //external authentication error
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult ExternalAuthenticationError(IEnumerable<string> errors)
    {
        return View(errors);
    }

    [HttpGet]
    [PublicStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> SetLanguage(
        [FromServices] AppConfig config,
        [FromServices] IUserFieldService userFieldService,
        string langCode, string returnUrl = default)
    {
        var language = await _languageService.GetLanguageByCode(langCode);
        if (!language?.Published ?? false)
            language = _workContext.WorkingLanguage;

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //language part in URL
        if (config.SeoFriendlyUrlsForLanguagesEnabled)
        {
            if (await IsLocalized(returnUrl, Request.PathBase))
                returnUrl = RemoveLanguageSeoCode(returnUrl, Request.PathBase);

            returnUrl = AddLanguageSeo(returnUrl, language);
        }

        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.LanguageId, language.Id,
            _workContext.CurrentStore.Id);

        //notification
        await _mediator.Publish(new ChangeLanguageEvent(_workContext.CurrentCustomer, language));

        return Redirect(returnUrl);
    }

    //Use in SlugRouteTransformer.
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult InternalRedirect(string url, bool permanentRedirect)
    {
        //ensure it's invoked from our GenericPathRoute class
        if (HttpContext.Items["grand.RedirectFromGenericPathRoute"] == null ||
            !Convert.ToBoolean(HttpContext.Items["grand.RedirectFromGenericPathRoute"]))
        {
            url = Url.RouteUrl("HomePage");
            permanentRedirect = false;
        }

        //home page
        if (string.IsNullOrEmpty(url))
        {
            url = Url.RouteUrl("HomePage");
            permanentRedirect = false;
        }

        //prevent open redirection attack
        if (!Url.IsLocalUrl(url))
        {
            url = Url.RouteUrl("HomePage");
            permanentRedirect = false;
        }

        url = Flurl.Url.EncodeIllegalCharacters(url);

        return permanentRedirect ? RedirectPermanent(url) : Redirect(url);
    }

    [DenySystemAccount]
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> SetCurrency(
        [FromServices] ICurrencyService currencyService,
        [FromServices] IUserFieldService userFieldService,
        string currencyCode, string returnUrl = "")
    {
        var currency = await currencyService.GetCurrencyByCode(currencyCode);
        if (currency != null)
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.CurrencyId,
                currency.Id, _workContext.CurrentStore.Id);

        //clear coupon code
        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.DiscountCoupons, "");

        //clear gift card
        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, "");

        //notification
        await _mediator.Publish(new ChangeCurrencyEvent(_workContext.CurrentCustomer, currency));

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        return Redirect(returnUrl);
    }

    [DenySystemAccount]
    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> SetStore(
        [FromServices] IStoreService storeService,
        [FromServices] IStoreHelper storeHelper,
        [FromServices] CommonSettings commonSettings,
        string shortcut, string returnUrl = "")
    {
        var currentstoreShortcut = _workContext.CurrentStore.Shortcut;
        if (currentstoreShortcut != shortcut)
            if (commonSettings.AllowToSelectStore)
            {
                var selectedstore = storeService.GetAll().FirstOrDefault(x =>
                    string.Equals(x.Shortcut, shortcut, StringComparison.InvariantCultureIgnoreCase));
                if (selectedstore != null)
                {
                    await storeHelper.SetStoreCookie(selectedstore.Id);

                    //notification
                    await _mediator.Publish(new ChangeStoreEvent(_workContext.CurrentCustomer, selectedstore));

                    if (selectedstore.Url != _workContext.CurrentStore.Url)
                        return Redirect(selectedstore.SslEnabled ? selectedstore.SecureUrl : selectedstore.Url);
                }
            }

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        return Redirect(returnUrl);
    }

    [DenySystemAccount]
    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> SetTaxType(
        [FromServices] TaxSettings taxSettings,
        [FromServices] IUserFieldService userFieldService,
        int customerTaxType, string returnUrl = default)
    {
        var taxDisplayType = (TaxDisplayType)Enum.ToObject(typeof(TaxDisplayType), customerTaxType);

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //whether customers are allowed to select tax display type
        if (!taxSettings.AllowCustomersToSelectTaxDisplayType)
            return Redirect(returnUrl);

        //save passed value
        await userFieldService.SaveField(_workContext.CurrentCustomer,
            SystemCustomerFieldNames.TaxDisplayTypeId, (int)taxDisplayType, _workContext.CurrentStore.Id);

        //notification
        await _mediator.Publish(new ChangeTaxTypeEvent(_workContext.CurrentCustomer, taxDisplayType));

        return Redirect(returnUrl);
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> SetStoreTheme(
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IThemeContextFactory themeContextFactory, string themeName, string returnUrl = "")
    {
        if (!storeInformationSettings.AllowCustomerToSelectTheme) return Redirect(returnUrl);

        var themeContext = themeContextFactory.GetThemeContext("");
        if (themeContext != null) await themeContext.SetTheme(themeName);

        //notification
        await _mediator.Publish(new ChangeThemeEvent(_workContext.CurrentCustomer, themeName));

        //home page
        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        return Redirect(returnUrl);
    }

    //sitemap page
    [HttpGet]
    public virtual async Task<IActionResult> Sitemap([FromServices] CommonSettings commonSettings)
    {
        if (!commonSettings.SitemapEnabled)
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetSitemap {
            Customer = _workContext.CurrentCustomer,
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    [ClosedStore(true)]
    [PublicStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> CookieAccept(bool accept,
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IUserFieldService userFieldService,
        [FromServices] ICookiePreference cookiePreference)
    {
        if (!storeInformationSettings.DisplayCookieInformation)
            //disabled
            return Json(new { stored = false });

        //save consent cookies
        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "",
            _workContext.CurrentStore.Id);
        var dictionary = new Dictionary<string, bool>();
        var consentCookies = cookiePreference.GetConsentCookies();
        foreach (var item in consentCookies.Where(x => x.AllowToDisable)) dictionary.Add(item.SystemName, accept);

        if (dictionary.Any())
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies,
                dictionary, _workContext.CurrentStore.Id);

        //save setting - CookieAccepted
        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.CookieAccepted,
            true, _workContext.CurrentStore.Id);

        return Json(new { stored = true });
    }

    [ClosedStore(true)]
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> PrivacyPreference([FromServices] StoreInformationSettings
        storeInformationSettings)
    {
        if (!storeInformationSettings.DisplayPrivacyPreference)
            //disabled
            return Json(new { html = "" });

        var model = await _mediator.Send(new GetPrivacyPreference {
            Customer = _workContext.CurrentCustomer,
            Store = _workContext.CurrentStore
        });

        return Json(new {
            html = await this.RenderPartialViewToString("PrivacyPreference", model, true),
            model
        });
    }

    [HttpPost]
    [ClosedStore(true)]
    [PublicStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> PrivacyPreference(IDictionary<string, string> model,
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IUserFieldService userFieldService,
        [FromServices] ICookiePreference cookiePreference)
    {
        if (!storeInformationSettings.DisplayPrivacyPreference)
            return Json(new { success = false });

        const string consent = "ConsentCookies";
        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "",
            _workContext.CurrentStore.Id);
        var selectedConsentCookies = new List<string>();
        foreach (var item in model)
            if (item.Key.StartsWith(consent))
                selectedConsentCookies.Add(item.Value);

        var dictionary = new Dictionary<string, bool>();
        var consentCookies = cookiePreference.GetConsentCookies();
        foreach (var item in consentCookies)
            if (item.AllowToDisable)
                dictionary.Add(item.SystemName, selectedConsentCookies.Contains(item.SystemName));

        await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies,
            dictionary, _workContext.CurrentStore.Id);

        return Json(new { success = true });
    }

    //robots.txt file
    [ClosedStore(true)]
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> RobotsTextFile()
    {
        var sb = await _mediator.Send(new GetRobotsTextFile { StoreId = _workContext.CurrentStore.Id });
        return Content(sb, "text/plain");
    }

    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult GenericUrl()
    {
        //not found
        return InvokeHttp404();
    }

    [ClosedStore(true)]
    [PublicStore(true)]
    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult StoreClosed()
    {
        return View();
    }

    [HttpPost]
    [ClosedStore(true)]
    [PublicStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> SaveCurrentPosition(
        LocationModel model,
        [FromServices] CustomerSettings customerSettings)
    {
        if (!customerSettings.GeoEnabled)
            return Content("");

        await _mediator.Send(new CurrentPositionCommand { Customer = _workContext.CurrentCustomer, Model = model });

        return Content("");
    }

    #endregion
}