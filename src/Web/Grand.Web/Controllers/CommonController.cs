using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Storage.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Themes;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Commands.Models.Common;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Events;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Grand.Web.Common.Controllers;

namespace Grand.Web.Controllers
{
    public partial class CommonController : BasePublicController
    {
        #region Fields

        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Constructors

        public CommonController(
            ITranslationService translationService,
            IWorkContext workContext,
            ILanguageService languageService,
            IMediator mediator,
            CaptchaSettings captchaSettings)
        {
            _translationService = translationService;
            _workContext = workContext;
            _languageService = languageService;
            _mediator = mediator;
            _captchaSettings = captchaSettings;
        }

        #endregion

        #region Utilities

        private string RemoveLanguageSeoCode(string url, PathString pathBase)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var _ = new PathString(url).StartsWithSegments(pathBase, out PathString resultpath);
            url = WebUtility.UrlDecode(resultpath);

            url = url.TrimStart('/');
            var result = url.Contains('/') ? url.Substring(url.IndexOf('/')) : string.Empty;

            result = pathBase + result;
            return result;
        }

        private async Task<bool> IsLocalized(string url, PathString pathBase)
        {
            var _ = new PathString(url).StartsWithSegments(pathBase, out PathString result);
            url = WebUtility.UrlDecode(result);

            var firstSegment = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(firstSegment))
                return false;

            //suppose that the first segment is the language code and try to get language
            var language = (await _languageService.GetAllLanguages())
                .FirstOrDefault(urlLanguage => urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

            return language != null ? language.Published : false;
        }
        private string AddLanguageSeo(string url, PathString pathBase, Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            //remove application path from raw URL
            if (!string.IsNullOrEmpty(url))
            {
                var _ = new PathString(url).StartsWithSegments(pathBase, out PathString result);
                url = WebUtility.UrlDecode(result);
            }

            //add language code
            url = $"/{language.UniqueSeoCode}{url}";
            url = pathBase + url;

            return url;
        }

        #endregion

        #region Methods

        //page not found
        public virtual IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            Response.ContentType = "text/html";
            return View();
        }

        //access denied
        public virtual IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            Response.ContentType = "text/html";
            return View();
        }

        //external authentication error
        public virtual IActionResult ExternalAuthenticationError(IEnumerable<string> errors)
        {
            return View(errors);
        }

        [PublicStore(true)]
        public virtual async Task<IActionResult> SetLanguage(
            [FromServices] AppConfig config,
            string langid, string returnUrl = "")
        {

            var language = await _languageService.GetLanguageById(langid);
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
                if (await IsLocalized(returnUrl, this.Request.PathBase))
                    returnUrl = RemoveLanguageSeoCode(returnUrl, this.Request.PathBase);

                returnUrl = AddLanguageSeo(returnUrl, this.Request.PathBase, language);
            }

            await _workContext.SetWorkingLanguage(language);

            return Redirect(returnUrl);
        }

        //helper method to redirect users.
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

            url = Uri.EscapeUriString(WebUtility.UrlDecode(url));

            if (permanentRedirect)
                return RedirectPermanent(url);
            return Redirect(url);
        }

        [PublicStore(true)]
        public virtual async Task<IActionResult> SetCurrency(
            [FromServices] ICurrencyService currencyService,
            [FromServices] IUserFieldService userFieldService,
            string customerCurrency, string returnUrl = "")
        {
            var currency = await currencyService.GetCurrencyById(customerCurrency);
            if (currency != null)
                await _workContext.SetWorkingCurrency(currency);

            //clear coupon code
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.DiscountCoupons, "");

            //clear gift card
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, "");

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> SetStore(
            [FromServices] IStoreService storeService,
            [FromServices] IStoreHelper _storeHelper,
            [FromServices] CommonSettings commonSettings,
            string store, string returnUrl = "")
        {
            var currentstoreid = _workContext.CurrentStore.Id;
            if (currentstoreid != store)
            {
                if (commonSettings.AllowToSelectStore)
                {
                    var selectedstore = await storeService.GetStoreById(store);
                    if (selectedstore != null)
                        await _storeHelper.SetStoreCookie(store);
                }
            }
            var prevStore = await storeService.GetStoreById(currentstoreid);
            var currStore = await storeService.GetStoreById(store);

            if (prevStore != null && currStore != null)
            {
                if (prevStore.Url != currStore.Url)
                {
                    return Redirect(currStore.SslEnabled ? currStore.SecureUrl : currStore.Url);
                }
            }

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> SetTaxType(int customerTaxType, string returnUrl = "")
        {
            var taxDisplayType = (TaxDisplayType)Enum.ToObject(typeof(TaxDisplayType), customerTaxType);
            await _workContext.SetTaxDisplayType(taxDisplayType);

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //contact us page
        //available even when a store is closed
        [ClosedStore(true)]
        public virtual async Task<IActionResult> ContactUs(
            [FromServices] StoreInformationSettings storeInformationSettings,
            [FromServices] IPageService pageService)
        {
            if (storeInformationSettings.StoreClosed)
            {
                var closestorepage = await pageService.GetPageBySystemName("ContactUs");
                if (closestorepage == null || !closestorepage.AccessibleWhenStoreClosed)
                    return RedirectToRoute("StoreClosed");
            }
            var model = await _mediator.Send(new ContactUsCommand()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        [HttpPost, ActionName("ContactUs")]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [ClosedStore(true)]
        public virtual async Task<IActionResult> ContactUsSend(
            [FromServices] StoreInformationSettings storeInformationSettings,
            [FromServices] IPageService pageService,
            ContactUsModel model, IFormCollection form, bool captchaValid)
        {
            if (storeInformationSettings.StoreClosed)
            {
                var closestorepage = await pageService.GetPageBySystemName("ContactUs");
                if (closestorepage == null || !closestorepage.AccessibleWhenStoreClosed)
                    return RedirectToRoute("StoreClosed");
            }

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new ContactUsSendCommand()
                {
                    CaptchaValid = captchaValid,
                    Form = form,
                    Model = model,
                    Store = _workContext.CurrentStore
                });

                if (result.errors.Any())
                {
                    foreach (var item in result.errors)
                    {
                        ModelState.AddModelError("", item);
                    }
                }
                else
                {
                    //notification
                    await _mediator.Publish(new ContactUsEvent(_workContext.CurrentCustomer, result.model, form));

                    model = result.model;
                    return View(model);
                }
            }
            model = await _mediator.Send(new ContactUsCommand()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Model = model,
                Form = form
            });

            return View(model);
        }


        //sitemap page
        public virtual async Task<IActionResult> Sitemap([FromServices] CommonSettings commonSettings)
        {
            if (!commonSettings.SitemapEnabled)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetSitemap()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        public virtual async Task<IActionResult> SetStoreTheme(
            [FromServices] IThemeContext themeContext, string themeName, string returnUrl = "")
        {
            await themeContext.SetWorkingTheme(themeName);

            //home page
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }


        [HttpPost]
        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual async Task<IActionResult> CookieAccept(bool accept,
            [FromServices] StoreInformationSettings storeInformationSettings,
            [FromServices] IUserFieldService userFieldService,
            [FromServices] ICookiePreference cookiePreference)
        {
            if (!storeInformationSettings.DisplayCookieInformation)
                //disabled
                return Json(new { stored = false });

            //save consentcookies
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "", _workContext.CurrentStore.Id);
            var dictionary = new Dictionary<string, bool>();
            var consentCookies = cookiePreference.GetConsentCookies();
            foreach (var item in consentCookies.Where(x => x.AllowToDisable))
            {
                dictionary.Add(item.SystemName, accept);
            }
            if (dictionary.Any())
                await userFieldService.SaveField<Dictionary<string, bool>>(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, dictionary, _workContext.CurrentStore.Id);

            //save setting - CookieAccepted
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.CookieAccepted, true, _workContext.CurrentStore.Id);

            return Json(new { stored = true });
        }

        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual async Task<IActionResult> PrivacyPreference([FromServices] StoreInformationSettings
            storeInformationSettings)
        {
            if (!storeInformationSettings.DisplayPrivacyPreference)
                //disabled
                return Json(new { html = "" });

            var model = await _mediator.Send(new GetPrivacyPreference()
            {
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore
            });

            return Json(new
            {
                html = await this.RenderPartialViewToString("PrivacyPreference", model, true)
            }); ;
        }

        [HttpPost]
        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual async Task<IActionResult> PrivacyPreference(IFormCollection form,
            [FromServices] StoreInformationSettings storeInformationSettings,
            [FromServices] IUserFieldService userFieldService,
            [FromServices] ICookiePreference _cookiePreference)
        {

            if (!storeInformationSettings.DisplayPrivacyPreference)
                return Json(new { success = false });

            var consent = "ConsentCookies";
            await userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, "", _workContext.CurrentStore.Id);
            var selectedConsentCookies = new List<string>();
            foreach (var item in form)
            {
                if (item.Key.StartsWith(consent))
                    selectedConsentCookies.Add(item.Value);
            }
            var dictionary = new Dictionary<string, bool>();
            var consentCookies = _cookiePreference.GetConsentCookies();
            foreach (var item in consentCookies)
            {
                if (item.AllowToDisable)
                    dictionary.Add(item.SystemName, selectedConsentCookies.Contains(item.SystemName));
            }

            await userFieldService.SaveField<Dictionary<string, bool>>(_workContext.CurrentCustomer, SystemCustomerFieldNames.ConsentCookies, dictionary, _workContext.CurrentStore.Id);

            return Json(new { success = true });
        }

        //robots.txt file
        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual async Task<IActionResult> RobotsTextFile()
        {
            var sb = await _mediator.Send(new GetRobotsTextFile());
            return Content(sb, "text/plain");
        }

        public virtual IActionResult GenericUrl()
        {
            //not found
            return InvokeHttp404();
        }

        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual IActionResult StoreClosed() => View();

        [HttpPost]
        public virtual async Task<IActionResult> ContactAttributeChange(IFormCollection form)
        {
            var result = await _mediator.Send(new ContactAttributeChangeCommand()
            {
                Form = form,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore
            });
            return Json(new
            {
                enabledattributeids = result.enabledAttributeIds.ToArray(),
                disabledattributeids = result.disabledAttributeIds.ToArray()
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> UploadFileContactAttribute(string attributeId,
            [FromServices] IDownloadService downloadService,
            [FromServices] IContactAttributeService contactAttributeService)
        {
            var attribute = await contactAttributeService.GetContactAttributeById(attributeId);
            if (attribute == null || attribute.AttributeControlType != AttributeControlType.FileUpload)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }
            var form = await HttpContext.Request.ReadFormAsync();
            var httpPostedFile = form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded",
                    downloadGuid = Guid.Empty,
                });
            }

            var fileBinary = httpPostedFile.GetDownloadBits();

            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (String.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();
            //remove path (passed in IE)
            fileName = Path.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
            {
                var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.ToLowerInvariant()
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                if(!allowedFileExtensions.Contains(fileExtension.ToLowerInvariant()))
                {
                    return Json(new
                    {
                        success = false,
                        message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                        downloadGuid = Guid.Empty,
                    });
                }
            }

            if (attribute.ValidationFileMaximumSize.HasValue)
            {
                //compare in bytes
                var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
                if (fileBinary.Length > maxFileSizeBytes)
                {
                    //when returning JSON the mime-type must be set to text/plain
                    //otherwise some browsers will pop-up a "Save As" dialog.
                    return Json(new
                    {
                        success = false,
                        message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"), attribute.ValidationFileMaximumSize.Value),
                        downloadGuid = Guid.Empty,
                    });
                }
            }

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = "",
                DownloadBinary = fileBinary,
                ContentType = contentType,
                //we store filename without extension for downloads
                Filename = Path.GetFileNameWithoutExtension(fileName),
                Extension = fileExtension,
                IsNew = true
            };

            await downloadService.InsertDownload(download);

            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new
            {
                success = true,
                message = _translationService.GetResource("ShoppingCart.FileUploaded"),
                downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
                downloadGuid = download.DownloadGuid,
            });
        }


        [HttpPost, ActionName("PopupInteractiveForm")]
        public virtual async Task<IActionResult> PopupInteractiveForm(IFormCollection formCollection)
        {
            var result = await _mediator.Send(new PopupInteractiveCommand() { Form = formCollection });
            return Json(new
            {
                success = !result.Any(),
                errors = result
            });
        }

        [HttpPost]
        [ClosedStore(true)]
        [PublicStore(true)]
        public virtual async Task<IActionResult> SaveCurrentPosition(
            LocationModel model,
            [FromServices] CustomerSettings customerSettings)
        {
            if (!customerSettings.GeoEnabled)
                return Content("");

            await _mediator.Send(new CurrentPositionCommand() { Customer = _workContext.CurrentCustomer, Model = model });

            return Content("");
        }

        #endregion
    }
}
