using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Payments;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Extensions.Mapping.Settings;
using Grand.Web.AdminShared.Models.Payments;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentController(
    IPaymentService paymentService,
    ISettingService settingService,
    ICountryService countryService,
    IShippingMethodService shippingMethodService,
    ITranslationService translationService,
    IContextAccessor contextAccessor) : BaseStoreController
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #region Payment methods

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> Methods()
    {
        var paymentSettings = await settingService.LoadSetting<PaymentSettings>(CurrentStoreId);

        var paymentMethodsModel = new List<PaymentMethodModel>();
        var paymentMethods = await paymentService.LoadAllPaymentMethods(storeId: CurrentStoreId);
        foreach (var paymentMethod in paymentMethods)
        {
            var tmp = await paymentMethod.ToModel();
            tmp.IsActive = paymentMethod.IsPaymentMethodActive(paymentSettings);
            paymentMethodsModel.Add(tmp);
        }

        var gridModel = new DataSourceResult {
            Data = paymentMethodsModel,
            Total = paymentMethodsModel.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> MethodUpdate(PaymentMethodModel model)
    {
        var paymentSettings = await settingService.LoadSetting<PaymentSettings>(CurrentStoreId);

        var pm = paymentService.LoadPaymentMethodBySystemName(model.SystemName);
        if (pm == null)
            return new JsonResult("");

        if (pm.IsPaymentMethodActive(paymentSettings))
        {
            if (!model.IsActive)
            {
                paymentSettings.ActivePaymentProviderSystemNames.Remove(pm.SystemName);
                await settingService.SaveSetting(paymentSettings, CurrentStoreId);
            }
        }
        else
        {
            if (model.IsActive)
            {
                paymentSettings.ActivePaymentProviderSystemNames.Add(pm.SystemName);
                await settingService.SaveSetting(paymentSettings, CurrentStoreId);
            }
        }

        return new JsonResult("");
    }

    #endregion

    #region Payment settings

    public async Task<IActionResult> Settings()
    {
        var paymentSettings = await settingService.LoadSetting<PaymentSettings>(CurrentStoreId);
        var model = paymentSettings.ToModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(PaymentSettingsModel model)
    {
        var paymentSettings = await settingService.LoadSetting<PaymentSettings>(CurrentStoreId);
        paymentSettings = model.ToEntity(paymentSettings);
        await settingService.SaveSetting(paymentSettings, CurrentStoreId);

        Success(translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #region Restrictions

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> MethodRestrictions()
    {
        var model = new PaymentMethodRestrictionModel();
        var paymentMethods = await paymentService.LoadAllPaymentMethods(storeId: CurrentStoreId);
        var countries = await countryService.GetAllCountries(showHidden: true);
        var shippings = await shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);

        foreach (var pm in paymentMethods) model.AvailablePaymentMethods.Add(await pm.ToModel());
        foreach (var c in countries) model.AvailableCountries.Add(c.ToModel());
        foreach (var s in shippings)
            model.AvailableShippingMethods.Add(new ShippingMethodModel {
                Id = s.Id,
                Name = s.Name
            });

        foreach (var pm in paymentMethods)
        {
            var restictedCountries = await paymentService.GetRestrictedCountryIds(pm, CurrentStoreId);
            foreach (var c in countries)
            {
                var resticted = restictedCountries.Contains(c.Id);
                if (!model.Resticted.ContainsKey(pm.SystemName))
                    model.Resticted[pm.SystemName] = new Dictionary<string, bool>();
                model.Resticted[pm.SystemName][c.Id] = resticted;
            }

            var restictedShipping = await paymentService.GetRestrictedShippingIds(pm, CurrentStoreId);
            foreach (var s in shippings)
            {
                var resticted = restictedShipping.Contains(s.Name);
                if (!model.RestictedShipping.ContainsKey(pm.SystemName))
                    model.RestictedShipping[pm.SystemName] = new Dictionary<string, bool>();
                model.RestictedShipping[pm.SystemName][s.Name] = resticted;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ActionName("MethodRestrictions")]
    [RequestFormLimits(ValueCountLimit = 2048)]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> MethodRestrictionsSave(IDictionary<string, string[]> model)
    {
        var paymentMethods = await paymentService.LoadAllPaymentMethods(storeId: CurrentStoreId);
        var countries = await countryService.GetAllCountries(showHidden: true);
        var shippings = await shippingMethodService.GetAllShippingMethods(storeId: CurrentStoreId);

        foreach (var pm in paymentMethods)
        {
            if (model.TryGetValue($"restrict_{pm.SystemName.Replace(".", "")}", out var countryIds))
            {
                var countryIdsToRestrict = countryIds.ToList();
                var newCountryIds =
                    (from c in countries where countryIdsToRestrict.Contains(c.Id) select c.Id).ToList();
                await paymentService.SaveRestrictedCountryIds(pm, newCountryIds, CurrentStoreId);
            }
            else
            {
                await paymentService.SaveRestrictedCountryIds(pm, new List<string>(), CurrentStoreId);
            }

            if (model.TryGetValue($"restrictship_{pm.SystemName.Replace(".", "")}", out var shipIds))
            {
                var shipIdsToRestrict = shipIds.ToList();
                var newShipIds = (from s in shippings where shipIdsToRestrict.Contains(s.Name) select s.Name).ToList();
                await paymentService.SaveRestrictedShippingIds(pm, newShipIds, CurrentStoreId);
            }
            else
            {
                await paymentService.SaveRestrictedShippingIds(pm, new List<string>(), CurrentStoreId);
            }
        }

        Success(translationService.GetResource("Admin.Configuration.Payment.MethodRestrictions.Updated"));
        await SaveSelectedTabIndex();
        return RedirectToAction("MethodRestrictions");
    }

    #endregion
}
