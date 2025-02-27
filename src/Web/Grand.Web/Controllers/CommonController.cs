using Grand.Business.Common.Services.Stores;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Themes;
using Grand.Web.Events;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class CommonController : BasePublicController
{
    #region Constructors

    public CommonController(IContextAccessor contextAccessor,
        ILanguageService languageService,
        IMediator mediator)
    {
        _contextAccessor = contextAccessor;
        _languageService = languageService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly ILanguageService _languageService;
    private readonly IContextAccessor _contextAccessor;
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

        var firstSegment = url.Split(['/'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
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
        [FromServices] ICustomerService customerService,
        string langCode, string returnUrl = "")
    {
        var language = await _languageService.GetLanguageByCode(langCode);
        if (language == null)
            return NotFound();

        if (!language.Published)
            language = _contextAccessor.WorkContext.WorkingLanguage;

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
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.LanguageId, language.Id, _contextAccessor.StoreContext.CurrentStore.Id);

        //notification
        await _mediator.Publish(new ChangeLanguageEvent(_contextAccessor.WorkContext.CurrentCustomer, language));

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
        [FromServices] ICustomerService customerService,
        string currencyCode, string returnUrl = "")
    {
        var currency = await currencyService.GetCurrencyByCode(currencyCode);
        if (currency != null)
            await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.CurrencyId,
                currency.Id, _contextAccessor.StoreContext.CurrentStore.Id);

        //clear coupon code
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.DiscountCoupons, "");

        //clear gift card
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, "");

        //notification
        await _mediator.Publish(new ChangeCurrencyEvent(_contextAccessor.WorkContext.CurrentCustomer, currency));

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
        [FromServices] CommonSettings commonSettings,
        [FromServices] SecurityConfig securityConfig,
        string shortcut, string returnUrl = "")
    {
        var currentstoreShortcut = _contextAccessor.StoreContext.CurrentStore.Shortcut;
        if (currentstoreShortcut != shortcut)
            if (commonSettings.AllowToSelectStore)
            {
                var selectedstore = (await storeService.GetAllStores()).FirstOrDefault(x =>
                    string.Equals(x.Shortcut, shortcut, StringComparison.InvariantCultureIgnoreCase));
                if (selectedstore != null)
                {
                    SetStoreCookie(selectedstore);

                    //notification
                    await _mediator.Publish(new ChangeStoreEvent(_contextAccessor.WorkContext.CurrentCustomer, selectedstore));

                    if (selectedstore.Url != _contextAccessor.StoreContext.CurrentStore.Url)
                        return Redirect(selectedstore.SslEnabled ? selectedstore.SecureUrl : selectedstore.Url);
                }
            }

        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        return Redirect(returnUrl);

        void SetStoreCookie(Store store)
        {
            if (store == null)
                return;

            //remove current cookie
            HttpContext.Response.Cookies.Delete(CommonHelper.StoreCookieName);

            //get date of cookie expiration
            var cookieExpiresDate = DateTime.UtcNow.AddHours(securityConfig.CookieAuthExpires);

            //set new cookie value
            var options = new CookieOptions {
                HttpOnly = true,
                Expires = cookieExpiresDate
            };
            HttpContext.Response.Cookies.Append(CommonHelper.StoreCookieName, store.Id, options);
        }
    }

    [DenySystemAccount]
    //available even when navigation is not allowed
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> SetTaxType(
        [FromServices] TaxSettings taxSettings,
        [FromServices] ICustomerService customerService,
        int customerTaxType, string returnUrl = "")
    {
        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        var taxDisplayType = (TaxDisplayType)Enum.ToObject(typeof(TaxDisplayType), customerTaxType);

        //whether customers are allowed to select tax display type
        if (!taxSettings.AllowCustomersToSelectTaxDisplayType)
            return Redirect(returnUrl);

        //save passed value
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
            SystemCustomerFieldNames.TaxDisplayTypeId, (int)taxDisplayType, _contextAccessor.StoreContext.CurrentStore.Id);

        //notification
        await _mediator.Publish(new ChangeTaxTypeEvent(_contextAccessor.WorkContext.CurrentCustomer, taxDisplayType));

        return Redirect(returnUrl);
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> SetStoreTheme(
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IThemeContextFactory themeContextFactory, string themeName, string returnUrl = "")
    {
        //prevent open redirection attack
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = Url.RouteUrl("HomePage");

        if (!storeInformationSettings.AllowCustomerToSelectTheme) return Redirect(returnUrl);

        var themeContext = themeContextFactory.GetThemeContext("");
        if (themeContext != null) await themeContext.SetTheme(themeName);

        //notification
        await _mediator.Publish(new ChangeThemeEvent(_contextAccessor.WorkContext.CurrentCustomer, themeName));

        return Redirect(returnUrl);
    }

    //sitemap page
    [HttpGet]
    public virtual async Task<IActionResult> Sitemap([FromServices] CommonSettings commonSettings)
    {
        if (!commonSettings.SitemapEnabled)
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetSitemap {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    [ClosedStore(true)]
    [PublicStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> CookieAccept(bool accept,
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] ICustomerService customerService,
        [FromServices] ICookiePreference cookiePreference)
    {
        if (!storeInformationSettings.DisplayCookieInformation)
            //disabled
            return Json(new { stored = false });

        //save consent cookies
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "",
            _contextAccessor.StoreContext.CurrentStore.Id);
        var consentCookies = cookiePreference.GetConsentCookies();
        var dictionary = consentCookies.Where(x => x.AllowToDisable).ToDictionary(item => item.SystemName, item => accept);

        if (dictionary.Any())
            await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies,
                dictionary, _contextAccessor.StoreContext.CurrentStore.Id);

        //save setting - CookieAccepted
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.CookieAccepted,
            true, _contextAccessor.StoreContext.CurrentStore.Id);

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
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return Json(new
        {
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
        [FromServices] ICustomerService customerService,
        [FromServices] ICookiePreference cookiePreference)
    {
        if (!storeInformationSettings.DisplayPrivacyPreference)
            return Json(new { success = false });

        const string consent = "ConsentCookies";
        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "",
            _contextAccessor.StoreContext.CurrentStore.Id);
        var selectedConsentCookies = new List<string>();
        foreach (var item in model)
            if (item.Key.StartsWith(consent))
                selectedConsentCookies.Add(item.Value);

        var dictionary = new Dictionary<string, bool>();
        var consentCookies = cookiePreference.GetConsentCookies();
        foreach (var item in consentCookies)
            if (item.AllowToDisable)
                dictionary.Add(item.SystemName, selectedConsentCookies.Contains(item.SystemName));

        await customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, dictionary, _contextAccessor.StoreContext.CurrentStore.Id);

        return Json(new { success = true });
    }

    //robots.txt file
    [ClosedStore(true)]
    [PublicStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> RobotsTextFile()
    {
        var sb = await _mediator.Send(new GetRobotsTextFile { StoreId = _contextAccessor.StoreContext.CurrentStore.Id });
        return Content(sb, "text/plain");
    }

    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult GenericUrl()
    {
        //not found
        return NotFound();
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

        await _mediator.Send(new CurrentPositionCommand { Customer = _contextAccessor.WorkContext.CurrentCustomer, Model = model });

        return Content("");
    }

    [AllowAnonymous]
    [IgnoreApi]
    [HttpGet]
    public virtual async Task<IActionResult> QueuedEmail([FromServices] IQueuedEmailService queuedEmailService, string emailId)
    {
        if (string.IsNullOrEmpty(emailId))
        {
            return GetTrackingPixel();
        }

        var isFromAdmin = Request.GetTypedHeaders().Referer?.ToString()?.Contains("admin/queuedemail/edit/",
            StringComparison.OrdinalIgnoreCase) ?? false;

        if (!isFromAdmin)
        {
            var queuedEmail = await queuedEmailService.GetQueuedEmailById(emailId);
            if (queuedEmail != null && queuedEmail.ReadOnUtc == null)
            {
                queuedEmail.ReadOnUtc = DateTime.UtcNow;
                await queuedEmailService.UpdateQueuedEmail(queuedEmail);
            }
        }

        return GetTrackingPixel();

        IActionResult GetTrackingPixel()
        {

            const string TRACKING_PIXEL = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
            return File(
                Convert.FromBase64String(TRACKING_PIXEL),
                "image/png",
                "pixel.png"
            );
        }

    }

    #endregion
}