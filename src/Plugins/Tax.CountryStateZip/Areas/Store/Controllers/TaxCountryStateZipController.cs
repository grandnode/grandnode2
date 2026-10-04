using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tax.CountryStateZip.Domain;
using Tax.CountryStateZip.Models;
using Tax.CountryStateZip.Services;

namespace Tax.CountryStateZip.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the country/state/zip tax provider.
///     A store owner can add, edit and delete tax rates, but only the ones that
///     belong to his own store (<see cref="CurrentStoreId" />). Records owned by
///     other stores or global (store = *) records are never returned nor mutated.
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[AutoValidateAntiforgeryToken]
[PermissionAuthorize(PermissionSystemName.TaxSettings)]
public class TaxCountryStateZipController : BaseController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ICountryService _countryService;
    private readonly ITaxCategoryService _taxCategoryService;
    private readonly ITaxRateService _taxRateService;

    public TaxCountryStateZipController(ITaxCategoryService taxCategoryService,
        ICountryService countryService,
        ITaxRateService taxRateService,
        IContextAccessor contextAccessor)
    {
        _taxCategoryService = taxCategoryService;
        _countryService = countryService;
        _taxRateService = taxRateService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current staff/store-manager is bound to.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public async Task<IActionResult> Configure()
    {
        var taxCategories = await _taxCategoryService.GetAllTaxCategories(CurrentStoreId);
        if (taxCategories.Count == 0)
            return Content("No tax categories can be loaded");

        var model = new TaxRateListModel {
            //the store owner can only add records for his own store, so no store selector is offered
            AddStoreId = CurrentStoreId
        };

        //tax categories
        foreach (var tc in taxCategories)
            model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id });

        //countries
        var countries = await _countryService.GetAllCountries(showHidden: true);
        foreach (var c in countries)
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });

        //states
        model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "" });
        var defaultCountry = countries.FirstOrDefault();
        if (defaultCountry != null)
        {
            var states = await _countryService.GetStateProvincesByCountryId(defaultCountry.Id);
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id });
        }

        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> RatesList(DataSourceRequest command)
    {
        //only the current store records - filtered and paged in the service layer
        var records = await _taxRateService.GetAllTaxRates(CurrentStoreId, command.Page - 1, command.PageSize);

        var taxRatesModel = new List<TaxRateModel>();
        foreach (var x in records)
        {
            var m = new TaxRateModel {
                Id = x.Id,
                StoreId = x.StoreId,
                TaxCategoryId = x.TaxCategoryId,
                CountryId = x.CountryId,
                StateProvinceId = x.StateProvinceId,
                Zip = x.Zip,
                Percentage = x.Percentage
            };
            //tax category
            var tc = await _taxCategoryService.GetTaxCategoryById(x.TaxCategoryId);
            m.TaxCategoryName = tc != null ? tc.Name : "";
            //country
            var c = await _countryService.GetCountryById(x.CountryId);
            m.CountryName = c != null ? c.Name : "Unavailable";
            //state
            var s = c?.StateProvinces.FirstOrDefault(z => z.Id == x.StateProvinceId);
            m.StateProvinceName = s != null ? s.Name : "*";
            //zip
            m.Zip = !string.IsNullOrEmpty(x.Zip) ? x.Zip : "*";
            taxRatesModel.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = taxRatesModel,
            Total = records.TotalCount
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> RateUpdate(TaxRateModel model)
    {
        var taxRate = await _taxRateService.GetTaxRateById(model.Id);
        //guard: a store owner can only edit his own store records
        if (taxRate == null || taxRate.StoreId != CurrentStoreId)
            return new JsonResult("");

        taxRate.Zip = model.Zip == "*" ? null : model.Zip;
        taxRate.Percentage = model.Percentage;
        await _taxRateService.UpdateTaxRate(taxRate);

        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> RateDelete(string id)
    {
        var taxRate = await _taxRateService.GetTaxRateById(id);
        //guard: a store owner can only delete his own store records
        if (taxRate == null || taxRate.StoreId != CurrentStoreId)
            return new JsonResult("");

        await _taxRateService.DeleteTaxRate(taxRate);

        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> AddTaxRate(TaxRateListModel model)
    {
        var taxRate = new TaxRate {
            //force the current store - the owner cannot create records for another store
            StoreId = CurrentStoreId,
            TaxCategoryId = model.AddTaxCategoryId,
            CountryId = model.AddCountryId,
            StateProvinceId = model.AddStateProvinceId,
            Zip = model.AddZip,
            Percentage = model.AddPercentage
        };
        await _taxRateService.InsertTaxRate(taxRate);

        return Json(new { Result = true });
    }
}
