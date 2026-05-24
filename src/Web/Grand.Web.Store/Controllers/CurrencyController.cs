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

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.Currencies)]
public class CurrencyController(
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    ITranslationService translationService,
    IStoreService storeService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

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
        var primaryStoreCurrencyId = currencySettings.PrimaryStoreCurrencyId;

        var store = await storeService.GetStoreById(storeId);
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

        if (currency.Id == currencySettings.PrimaryStoreCurrencyId)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CantDeletePrimary") });

        var storeId = CurrentStoreId;

        var store = await storeService.GetStoreById(storeId);
        if (store?.DefaultCurrencyId == currency.Id)
            return Json(new { success = false, message = translationService.GetResource("Admin.Configuration.Currencies.CantUnassignDefault") });

        if (currency.Stores.Remove(storeId))
            await currencyService.UpdateCurrency(currency);

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
