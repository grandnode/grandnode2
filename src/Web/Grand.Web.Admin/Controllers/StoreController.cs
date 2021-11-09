﻿using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Stores;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Stores)]
    public partial class StoreController : BaseAdminController
    {
        private readonly IStoreViewModelService _storeViewModelService;
        private readonly IStoreService _storeService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;

        public StoreController(
            IStoreViewModelService storeViewModelService,
            IStoreService storeService,
            ILanguageService languageService,
            ITranslationService translationService)
        {
            _storeViewModelService = storeViewModelService;
            _storeService = storeService;
            _languageService = languageService;
            _translationService = translationService;
        }

        public IActionResult List() => View();

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var storeModels = (await _storeService.GetAllStores())
                .Select(x => x.ToModel())
                .ToList();

            var gridModel = new DataSourceResult {
                Data = storeModels,
                Total = storeModels.Count()
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create()
        {
            var model = _storeViewModelService.PrepareStoreModel();
            //locales
            await AddLocales(_languageService, model.Locales);
            //languages
            await _storeViewModelService.PrepareLanguagesModel(model);
            //warehouses
            await _storeViewModelService.PrepareWarehouseModel(model);
            //countries
            await _storeViewModelService.PrepareCountryModel(model);
            //currencies
            await _storeViewModelService.PrepareCurrencyModel(model);

            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> Create(StoreModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var store = await _storeViewModelService.InsertStoreModel(model);
                Success(_translationService.GetResource("Admin.Configuration.Stores.Added"));
                //selected tab
                await SaveSelectedTabIndex();

                return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
            }
            //languages
            await _storeViewModelService.PrepareLanguagesModel(model);
            //warehouses
            await _storeViewModelService.PrepareWarehouseModel(model);
            //countries
            await _storeViewModelService.PrepareCountryModel(model);
            //currencies
            await _storeViewModelService.PrepareCurrencyModel(model);

            //If we got this far, something failed, redisplay form
            return View(model);
        }
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var store = await _storeService.GetStoreById(id);
            if (store == null)
                //No store found with the specified id
                return RedirectToAction("List");

            var model = store.ToModel();
            //languages
            await _storeViewModelService.PrepareLanguagesModel(model);
            //warehouses
            await _storeViewModelService.PrepareWarehouseModel(model);
            //countries
            await _storeViewModelService.PrepareCountryModel(model);
            //currencies
            await _storeViewModelService.PrepareCurrencyModel(model);

            //locales
            await AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = store.GetTranslation(x => x.Name, languageId, false);
                locale.Shortcut = store.GetTranslation(x => x.Shortcut, languageId, false);
            });
            return View(model);
        }

        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(StoreModel model, bool continueEditing)
        {
            var store = await _storeService.GetStoreById(model.Id);
            if (store == null)
                //No store found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                store = await _storeViewModelService.UpdateStoreModel(store, model);
                Success(_translationService.GetResource("Admin.Configuration.Stores.Updated"));
                //selected tab
                await SaveSelectedTabIndex();

                return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //languages
            await _storeViewModelService.PrepareLanguagesModel(model);
            //warehouses
            await _storeViewModelService.PrepareWarehouseModel(model);
            //countries
            await _storeViewModelService.PrepareCountryModel(model);
            //currencies
            await _storeViewModelService.PrepareCurrencyModel(model);

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var store = await _storeService.GetStoreById(id);
            if (store == null)
                //No store found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _storeViewModelService.DeleteStore(store);
                Success(_translationService.GetResource("Admin.Configuration.Stores.Deleted"));
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", new { id = store.Id });
        }

        #region Domains

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> Domains(string storeId)
        {
            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
                //No store found with the specified id
                return ErrorForKendoGridJson("Store not found");

            var model = store.ToModel();

            var gridModel = new DataSourceResult {
                Data = model.Domains.ToList(),
                Total = model.Domains.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DomainInsert(string storeId, DomainHostModel model)
        {
            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
                //No store found with the specified id
                return ErrorForKendoGridJson("Store not found");

            if (ModelState.IsValid)
            {
                await _storeViewModelService.InsertDomainHostModel(store, model);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DomainUpdate(string storeId, DomainHostModel model)
        {
            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
                //No store found with the specified id
                return ErrorForKendoGridJson("Store not found");

            var domain = store.Domains.FirstOrDefault(x => x.Id == model.Id);
            if (domain == null)
                return ErrorForKendoGridJson("Domain not found");

            if (domain.Primary)
                return ErrorForKendoGridJson("You can't edit primary domain");

            if (ModelState.IsValid)
            {
                await _storeViewModelService.UpdateDomainHostModel(store, model);
                return new JsonResult("");
            }
            return ErrorForKendoGridJson(ModelState);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DomainDelete(string storeId, string id)
        {
            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
                //No store found with the specified id
                return ErrorForKendoGridJson("Store not found");

            var domain = store.Domains.FirstOrDefault(x => x.Id == id);
            if (domain == null)
                return ErrorForKendoGridJson("Domain not found");

            if (domain.Primary)
                return ErrorForKendoGridJson("You can't delete primary domain");

            if (ModelState.IsValid)
            {
                await _storeViewModelService.DeleteDomainHostModel(store, id);
                return new JsonResult("");
            }

            return ErrorForKendoGridJson(ModelState);
        }
        #endregion
    }
}
