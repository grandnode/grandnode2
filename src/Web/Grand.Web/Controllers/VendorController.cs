using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Vendors;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Controllers;

public class VendorController : BasePublicController
{
    #region Constructors

    public VendorController(
        IWorkContext workContext,
        ITranslationService translationService,
        ICustomerService customerService,
        IMessageProviderService messageProviderService,
        IVendorService vendorService,
        ISlugService slugService,
        IPictureService pictureService,
        ICountryService countryService,
        IMediator mediator,
        LanguageSettings languageSettings,
        VendorSettings vendorSettings,
        CaptchaSettings captchaSettings,
        CommonSettings commonSettings)
    {
        _workContext = workContext;
        _translationService = translationService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _vendorService = vendorService;
        _slugService = slugService;
        _pictureService = pictureService;
        _countryService = countryService;
        _mediator = mediator;
        _languageSettings = languageSettings;
        _vendorSettings = vendorSettings;
        _captchaSettings = captchaSettings;
        _commonSettings = commonSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task UpdatePictureSeoNames(Domain.Vendors.Vendor vendor)
    {
        var picture = await _pictureService.GetPictureById(vendor.PictureId);
        if (picture != null)
            await _pictureService.SetSeoFilename(picture, _pictureService.GetPictureSeName(vendor.Name));
    }

    #endregion

    #region Fields

    private readonly IWorkContext _workContext;
    private readonly ITranslationService _translationService;
    private readonly ICustomerService _customerService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IVendorService _vendorService;
    private readonly ISlugService _slugService;
    private readonly IPictureService _pictureService;
    private readonly ICountryService _countryService;
    private readonly IMediator _mediator;
    private readonly LanguageSettings _languageSettings;
    private readonly VendorSettings _vendorSettings;
    private readonly CaptchaSettings _captchaSettings;
    private readonly CommonSettings _commonSettings;

    #endregion

    #region Methods

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> ApplyVendor()
    {
        if (!_vendorSettings.AllowCustomersToApplyForVendorAccount)
            return RedirectToRoute("HomePage");

        var model = new ApplyVendorModel();
        if (!string.IsNullOrEmpty(_workContext.CurrentCustomer.VendorId))
        {
            //already applied for vendor account
            model.DisableFormInput = true;
            model.Result = _translationService.GetResource("Vendors.ApplyAccount.AlreadyApplied");
            return View(model);
        }

        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnApplyVendorPage;
        model.Email = _workContext.CurrentCustomer.Email;
        model.TermsOfServiceEnabled = _vendorSettings.TermsOfServiceEnabled;
        model.TermsOfServicePopup = _commonSettings.PopupForTermsOfServiceLinks;
        var countries =
            await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
        model.Address = await _mediator.Send(new GetVendorAddress {
            Language = _workContext.WorkingLanguage,
            Address = null,
            ExcludeProperties = false,
            PrePopulateWithCustomerFields = true,
            Customer = _workContext.CurrentCustomer,
            LoadCountries = () => countries
        });

        return View(model);
    }

    [HttpPost]
    [ActionName("ApplyVendor")]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> ApplyVendorSubmit(ApplyVendorModel model)
    {
        if (!_vendorSettings.AllowCustomersToApplyForVendorAccount)
            return RedirectToRoute("HomePage");

        if (ModelState.IsValid)
        {
            var description = FormatText.ConvertText(model.Description);
            //disabled by default
            var vendor = new Domain.Vendors.Vendor {
                Name = model.Name,
                Email = model.Email,
                Description = description,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = _vendorSettings.DefaultVendorPageSizeOptions,
                AllowCustomerReviews = _vendorSettings.DefaultAllowCustomerReview
            };
            model.Address.ToEntity(vendor.Address);

            await _vendorService.InsertVendor(vendor);

            //search engine name (the same as vendor name)                
            var seName = await vendor.ValidateSeName(vendor.Name, vendor.Name, true,
                HttpContext.RequestServices.GetRequiredService<SeoSettings>(),
                HttpContext.RequestServices.GetRequiredService<ISlugService>(),
                HttpContext.RequestServices.GetRequiredService<ILanguageService>());
            await _slugService.SaveSlug(vendor, seName, "");

            vendor.SeName = seName;
            await _vendorService.UpdateVendor(vendor);

            //associate to the current customer
            //but a store owner will have to manually activate this vendor
            //if he wants to grant access to admin area
            _workContext.CurrentCustomer.VendorId = vendor.Id;
            await _customerService.UpdateCustomerField(_workContext.CurrentCustomer.Id, x => x.VendorId,
                _workContext.CurrentCustomer.VendorId);

            //notify store owner here (email)
            await _messageProviderService.SendNewVendorAccountApplyStoreOwnerMessage(_workContext.CurrentCustomer,
                vendor, _workContext.CurrentStore, _languageSettings.DefaultAdminLanguageId);

            model.DisableFormInput = true;
            model.Result = _translationService.GetResource("Vendors.ApplyAccount.Submitted");
            return View(model);
        }

        //If we got this far, something failed, redisplay form
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnApplyVendorPage;
        model.TermsOfServiceEnabled = _vendorSettings.TermsOfServiceEnabled;
        model.TermsOfServicePopup = _commonSettings.PopupForTermsOfServiceLinks;
        var countries =
            await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
        model.Address = await _mediator.Send(new GetVendorAddress {
            Language = _workContext.WorkingLanguage,
            Address = null,
            Model = model.Address,
            ExcludeProperties = false,
            PrePopulateWithCustomerFields = true,
            Customer = _workContext.CurrentCustomer,
            LoadCountries = () => countries
        });
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ContactVendor(ContactVendorModel model)
    {
        if (!_vendorSettings.AllowCustomersToContactVendors)
            return Content("");

        var vendor = await _vendorService.GetVendorById(model.VendorId);
        if (vendor is not { Active: true } || vendor.Deleted)
            return Content("");

        if (ModelState.IsValid)
        {
            model = await _mediator.Send(new ContactVendorSendCommand {
                Model = model, Vendor = vendor, Store = _workContext.CurrentStore,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            return Json(model);
        }

        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage;
        model.Result = string.Join(",", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage));
        model.SuccessfullySent = false;

        return Json(model);
    }

    #endregion
}