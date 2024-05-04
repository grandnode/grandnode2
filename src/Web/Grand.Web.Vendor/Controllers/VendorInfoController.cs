using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Models.Common;
using Grand.Web.Vendor.Models.Vendor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Vendor.Controllers;

public class VendorInfoController : BaseVendorController
{
    #region Constructors

    public VendorInfoController(
        ITranslationService translationService,
        IVendorService vendorService,
        ILanguageService languageService,
        IWorkContext workContext,
        ICountryService countryService,
        ISlugService slugService,
        SeoSettings seoSettings,
        VendorSettings vendorSettings)
    {
        _translationService = translationService;
        _vendorService = vendorService;
        _languageService = languageService;
        _workContext = workContext;
        _countryService = countryService;
        _slugService = slugService;
        _seoSettings = seoSettings;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Fields

    private readonly IWorkContext _workContext;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly ILanguageService _languageService;
    private readonly ICountryService _countryService;
    private readonly ISlugService _slugService;

    private readonly SeoSettings _seoSettings;
    private readonly VendorSettings _vendorSettings;

    #endregion

    #region Private methods

    private async Task PrepareVendorAddressModel(VendorModel model, Domain.Vendors.Vendor vendor)
    {
        model.Address ??= new AddressModel();

        model.Address.FirstNameEnabled = false;
        model.Address.FirstNameRequired = false;
        model.Address.LastNameEnabled = false;
        model.Address.LastNameRequired = false;
        model.Address.EmailEnabled = false;
        model.Address.EmailRequired = false;
        model.Address.CompanyEnabled = _vendorSettings.AddressSettings.CompanyEnabled;
        model.Address.CountryEnabled = _vendorSettings.AddressSettings.CountryEnabled;
        model.Address.StateProvinceEnabled = _vendorSettings.AddressSettings.StateProvinceEnabled;
        model.Address.CityEnabled = _vendorSettings.AddressSettings.CityEnabled;
        model.Address.CityRequired = _vendorSettings.AddressSettings.CityRequired;
        model.Address.StreetAddressEnabled = _vendorSettings.AddressSettings.StreetAddressEnabled;
        model.Address.StreetAddressRequired = _vendorSettings.AddressSettings.StreetAddressRequired;
        model.Address.StreetAddress2Enabled = _vendorSettings.AddressSettings.StreetAddress2Enabled;
        model.Address.ZipPostalCodeEnabled = _vendorSettings.AddressSettings.ZipPostalCodeEnabled;
        model.Address.ZipPostalCodeRequired = _vendorSettings.AddressSettings.ZipPostalCodeRequired;
        model.Address.PhoneEnabled = _vendorSettings.AddressSettings.PhoneEnabled;
        model.Address.PhoneRequired = _vendorSettings.AddressSettings.PhoneRequired;
        model.Address.FaxEnabled = _vendorSettings.AddressSettings.FaxEnabled;
        model.Address.FaxRequired = _vendorSettings.AddressSettings.FaxRequired;
        model.Address.NoteEnabled = _vendorSettings.AddressSettings.NoteEnabled;
        model.Address.AddressTypeEnabled = false;

        //address
        model.Address.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = vendor != null && c.Id == vendor.Address.CountryId });

        var states = !string.IsNullOrEmpty(model.Address.CountryId)
            ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem {
                    Text = s.Name, Value = s.Id, Selected = vendor != null && s.Id == vendor.Address.StateProvinceId
                });
    }

    private async Task UpdateVendorModel(Domain.Vendors.Vendor vendor, VendorModel model)
    {
        vendor = model.ToEntity(vendor);
        vendor.Locales =
            await model.Locales.ToTranslationProperty(vendor, x => x.Name, _seoSettings, _slugService,
                _languageService);
        model.SeName = await vendor.ValidateSeName(model.SeName, vendor.Name, true, _seoSettings, _slugService,
            _languageService);
        vendor.Address = model.Address.ToEntity();
        vendor.SeName = model.SeName;

        await _vendorService.UpdateVendor(vendor);

        //search engine name                
        await _slugService.SaveSlug(vendor, model.SeName, "");
    }

    #endregion

    #region Methods

    //edit
    public async Task<IActionResult> Edit()
    {
        if (!_vendorSettings.AllowVendorsToEditInfo)
            throw new Exception("Vendor can't edit info");

        var vendor = await _vendorService.GetVendorById(_workContext.CurrentVendor.Id);
        if (vendor == null || vendor.Deleted)
            throw new ArgumentNullException(nameof(vendor));

        var model = vendor.ToModel();

        //locales
        await AddLocales(_languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = vendor.GetTranslation(x => x.Name, languageId, false);
            locale.Description = vendor.GetTranslation(x => x.Description, languageId, false);
            locale.MetaKeywords = vendor.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = vendor.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = vendor.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = vendor.GetSeName(languageId, false);
        });

        //prepare address model
        await PrepareVendorAddressModel(model, vendor);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VendorModel model)
    {
        if (!_vendorSettings.AllowVendorsToEditInfo)
            throw new Exception("Vendor can't edit info");

        var vendor = await _vendorService.GetVendorById(model.Id);
        if (vendor == null || vendor.Deleted || vendor.Id != _workContext.CurrentVendor.Id)
            throw new ArgumentNullException(nameof(vendor));

        if (ModelState.IsValid)
        {
            await UpdateVendorModel(vendor, model);

            Success(_translationService.GetResource("Vendor.Updated"));
            return RedirectToAction("Edit");
        }

        //If we got this far, something failed, redisplay form
        //prepare address model
        await PrepareVendorAddressModel(model, vendor);
        return View(model);
    }

    #endregion
}