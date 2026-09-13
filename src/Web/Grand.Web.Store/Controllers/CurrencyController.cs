using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Models;
using Microsoft.AspNetCore.Mvc;
using DomainStore = Grand.Domain.Stores.Store;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Currencies)]
public class CurrencyController(
    ICurrencyService currencyService,
    ISettingService settingService,
    ITranslationService translationService,
    IStoreService storeService,
    IProductService productService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    /// <summary>
    ///     The currency this store's prices are stored in - its own override where it has one, the global value
    ///     otherwise. Loaded for the staff store explicitly rather than taken from the injected settings, which are
    ///     resolved for the store the request itself was routed to.
    /// </summary>
    private async Task<string> EffectivePrimaryCurrencyId()
    {
        return (await settingService.LoadSetting<PrimaryCurrencySettings>(CurrentStoreId)).CurrencyId;
    }

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> ListData()
    {
        var storeId = CurrentStoreId;

        var store = await storeService.GetStoreById(storeId);
        var primaryStoreCurrencyId = await EffectivePrimaryCurrencyId();
        var defaultCurrencyId = store?.DefaultCurrencyId;

        var currencies = await currencyService.GetAllCurrencies(showHidden: false);

        var items = currencies
            .Select(c => new StoreCurrencyModel {
                Id = c.Id,
                Name = c.Name,
                CurrencyCode = c.CurrencyCode,
                Published = c.Published,
                DisplayOrder = c.DisplayOrder,
                LimitedToStores = c.LimitedToStores,
                IsAssignedToCurrentStore = !c.LimitedToStores || c.Stores.Contains(storeId),
                IsPrimaryStoreCurrency = c.Id == primaryStoreCurrencyId,
                IsDefaultStoreCurrency = c.Id == defaultCurrencyId,
                CanManage = c.LimitedToStores
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = items.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AssignStore(string id)
    {
        var currency = await currencyService.GetCurrencyById(id);
        if (currency == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotFound") });

        if (!currency.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CannotModifyGlobal") });

        var storeId = CurrentStoreId;
        if (!currency.Stores.Contains(storeId))
        {
            currency.Stores.Add(storeId);
            await currencyService.UpdateCurrency(currency);
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnassignStore(string id)
    {
        var currency = await currencyService.GetCurrencyById(id);
        if (currency == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotFound") });

        if (!currency.LimitedToStores)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CannotModifyGlobal") });

        var storeId = CurrentStoreId;

        var store = await storeService.GetStoreById(storeId);

        //the currency prices are stored in cannot leave the store
        if (currency.Id == await EffectivePrimaryCurrencyId())
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CantUnassignPrimary") });

        if (store?.DefaultCurrencyId == currency.Id)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CantUnassignDefault") });

        //the store must keep at least one available currency, otherwise its work context cannot be built
        var storeCurrencies = await currencyService.GetAllCurrencies(storeId: storeId);
        if (!storeCurrencies.Any(c => c.Id != currency.Id))
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CantUnassignLast") });

        if (currency.Stores.Remove(storeId))
            await currencyService.UpdateCurrency(currency);

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SetPrimaryCurrency(string id, bool confirmed)
    {
        var currency = await currencyService.GetCurrencyById(id);
        if (currency == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotFound") });

        if (!currency.Published)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotPublished") });

        var storeId = CurrentStoreId;

        if (currency.LimitedToStores && !currency.Stores.Contains(storeId))
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotAssignedToStore") });

        var store = await storeService.GetStoreById(storeId);
        if (store == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Stores.NotFound") });

        //product prices are stored in the primary currency and are not recalculated - a product shared with
        //another store would silently change its meaning, so the store owner has to confirm it
        if (!confirmed)
        {
            var sharedProducts = await productService.CountSharedProducts(storeId);
            if (sharedProducts > 0)
                return Json(new {
                    success = false,
                    requiresConfirmation = true,
                    message = string.Format(
                        translationService.GetResource("Admin.Configuration.Currencies.PrimaryCurrency.SharedProducts"),
                        sharedProducts)
                });
        }

        var primaryCurrencySettings = await settingService.LoadSetting<PrimaryCurrencySettings>(storeId);
        primaryCurrencySettings.CurrencyId = currency.Id;
        await settingService.SaveSetting(primaryCurrencySettings, storeId);
        await settingService.ClearCache();

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SetDefaultCurrency(string id)
    {
        var currency = await currencyService.GetCurrencyById(id);
        if (currency == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotFound") });

        if (!currency.Published)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotPublished") });

        var storeId = CurrentStoreId;

        if (currency.LimitedToStores && !currency.Stores.Contains(storeId))
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotAssignedToStore") });

        var store = await storeService.GetStoreById(storeId);
        if (store == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Stores.NotFound") });

        store.DefaultCurrencyId = currency.Id;
        await storeService.UpdateStore(store);

        return Json(new { success = true });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UnsetDefaultCurrency(string id)
    {
        var currency = await currencyService.GetCurrencyById(id);
        if (currency == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotFound") });

        var storeId = CurrentStoreId;
        var store = await storeService.GetStoreById(storeId);
        if (store == null)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Stores.NotFound") });

        if (store.DefaultCurrencyId != currency.Id)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.NotDefaultCurrency") });

        store.DefaultCurrencyId = string.Empty;
        await storeService.UpdateStore(store);

        return Json(new { success = true });
    }
}
