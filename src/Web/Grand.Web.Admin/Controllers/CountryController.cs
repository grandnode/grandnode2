﻿using Grand.Business.Common.Services.ExportImport;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Country;
using Grand.Web.Admin.Models.Directory;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Countries)]
    public partial class CountryController : BaseAdminController
    {
        #region Fields

        private readonly ICountryService _countryService;
        private readonly ICountryViewModelService _countryViewModelService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IExportManager<CountryStates> _exportManager;
        private readonly IImportManager<CountryStates> _importManager;

        #endregion

        #region Constructors

        public CountryController(ICountryService countryService,
            ICountryViewModelService countryViewModelService,
            ITranslationService translationService,
            ILanguageService languageService,
            IExportManager<CountryStates> exportManager,
            IImportManager<CountryStates> importManager)
        {
            _countryService = countryService;
            _countryViewModelService = countryViewModelService;
            _translationService = translationService;
            _languageService = languageService;
            _exportManager = exportManager;
            _importManager = importManager;
        }

        #endregion

        #region Countries

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List()
        {
            var model = new CountriesListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> CountryList(DataSourceRequest command, CountriesListModel countriesListModel)
        {
            var countries = await _countryService.GetAllCountries(showHidden: true);

            //Filters Countries based off of name
            if (!string.IsNullOrEmpty(countriesListModel.CountryName))
            {
                countries = countries.Where(
                    x => x.Name.ToLowerInvariant().Contains(countriesListModel.CountryName.ToLowerInvariant())
                    ).ToList();
            }

            var gridModel = new DataSourceResult {
                Data = countries.Select(x => x.ToModel()),
                Total = countries.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = _countryViewModelService.PrepareCountryModel();
            //locales
            await AddLocales(_languageService, model.Locales);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CountryModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var country = await _countryViewModelService.InsertCountryModel(model);
                Success(_translationService.GetResource("Admin.Configuration.Countries.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = country.Id }) : RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var country = await _countryService.GetCountryById(id);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            var model = country.ToModel();
            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = country.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CountryModel model, bool continueEditing)
        {
            var country = await _countryService.GetCountryById(model.Id);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                country = await _countryViewModelService.UpdateCountryModel(country, model);
                Success(_translationService.GetResource("Admin.Configuration.Countries.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    await SaveSelectedTabIndex();

                    return RedirectToAction("Edit", new { id = country.Id });
                }
                return RedirectToAction("List");
            }
            //If we got this far, something failed, redisplay form

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var country = await _countryService.GetCountryById(id);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            try
            {
                if (ModelState.IsValid)
                {
                    await _countryService.DeleteCountry(country);
                    Success(_translationService.GetResource("Admin.Configuration.Countries.Deleted"));
                    return RedirectToAction("List");
                }
                Error(ModelState);
                return RedirectToAction("Edit", new { id });
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("Edit", new { id = country.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> PublishSelected(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                var countries = await _countryService.GetCountriesByIds(selectedIds.ToArray());
                foreach (var country in countries)
                {
                    country.Published = true;
                    await _countryService.UpdateCountry(country);
                }
            }

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> UnpublishSelected(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                var countries = await _countryService.GetCountriesByIds(selectedIds.ToArray());
                foreach (var country in countries)
                {
                    country.Published = false;
                    await _countryService.UpdateCountry(country);
                }
            }
            return Json(new { Result = true });
        }

        #endregion

        #region States / provinces

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> States(string countryId, DataSourceRequest command)
        {
            var country = await _countryService.GetCountryById(countryId);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            var states = country.StateProvinces.ToList();

            var gridModel = new DataSourceResult {
                Data = states.Select(x => new { Id = x.Id, Name = x.Name, Abbreviation = x.Abbreviation, Published = x.Published, DisplayOrder = x.DisplayOrder }),
                Total = states.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        //create
        public async Task<IActionResult> StateCreatePopup(string countryId)
        {
            var model = _countryViewModelService.PrepareStateProvinceModel(countryId);
            //locales
            await AddLocales(_languageService, model.Locales);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> StateCreatePopup(StateProvinceModel model)
        {
            var country = await _countryService.GetCountryById(model.CountryId);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _countryViewModelService.InsertStateProvinceModel(model);
                return Content("");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        //edit
        public async Task<IActionResult> StateEditPopup(string id, string countryId)
        {
            var country = await _countryService.GetCountryById(countryId);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            var sp = country.StateProvinces.FirstOrDefault(x => x.Id == id);
            if (sp == null)
                //No state found with the specified id
                return RedirectToAction("List");

            var model = sp.ToModel();
            model.CountryId = country.Id;

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = sp.GetTranslation(x => x.Name, languageId, false);
            });

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> StateEditPopup(StateProvinceModel model)
        {
            var country = await _countryService.GetCountryById(model.CountryId);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            var sp = country.StateProvinces.FirstOrDefault(x => x.Id == model.Id);
            if (sp == null)
                //No state found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _countryViewModelService.UpdateStateProvinceModel(sp, model);
                return Content("");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> StateDelete(string id, string countryId)
        {
            var country = await _countryService.GetCountryById(countryId);
            if (country == null)
                //No country found with the specified id
                return RedirectToAction("List");

            var state = country.StateProvinces.FirstOrDefault(x => x.Id == id);
            if (state == null)
                throw new ArgumentException("No state found with the specified id");

            if (ModelState.IsValid)
            {
                await _countryService.DeleteStateProvince(state, countryId);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        #endregion

        #region Export / import

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        public async Task<IActionResult> ExportExcel()
        {
            var countries = await _countryService.GetAllCountries();
            var query = from p in countries
                        from s in p.StateProvinces
                        select new CountryStates() {
                            Country = p.TwoLetterIsoCode,
                            StateProvinceName = s.Name,
                            Abbreviation = s.Abbreviation,
                            DisplayOrder = s.DisplayOrder,
                            Published = s.Published
                        };

            var bytes = await _exportManager.Export(query);
            return File(bytes, "text/xls", "country.xlsx");
        }

        [PermissionAuthorizeAction(PermissionActionName.Import)]
        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile importexcelfile)
        {
            try
            {
                if (importexcelfile != null && importexcelfile.Length > 0)
                {
                    await _importManager.Import(importexcelfile.OpenReadStream());

                    Success(_translationService.GetResource("Admin.Configuration.Countries.ImportSuccess"));
                    return RedirectToAction("List");
                }
                Error(_translationService.GetResource("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                Error(exc);
                return RedirectToAction("List");
            }
        }

        #endregion
    }
}
