using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Extensions.Mapping.Settings;
using Grand.Web.Admin.Models.Payments;
using Grand.Web.Admin.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentController : BaseAdminController
{
    #region Constructors

    public PaymentController(IPaymentService paymentService,
        ISettingService settingService,
        ICountryService countryService,
        IShippingMethodService shippingMethodService,
        ITranslationService translationService,
        IServiceProvider serviceProvider,
        IWorkContext workContext)
    {
        _paymentService = paymentService;
        _settingService = settingService;
        _countryService = countryService;
        _shippingMethodService = shippingMethodService;
        _translationService = translationService;
        _serviceProvider = serviceProvider;
        _workContext = workContext;
    }

    #endregion

    #region Fields

    private readonly IPaymentService _paymentService;
    private readonly ISettingService _settingService;
    private readonly ICountryService _countryService;
    private readonly IShippingMethodService _shippingMethodService;
    private readonly ITranslationService _translationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkContext _workContext;

    #endregion

    #region Methods

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Methods()
    {
        var storeScope = await GetActiveStore();

        var _paymentSettings = _settingService.LoadSetting<PaymentSettings>(storeScope);

        var paymentMethodsModel = new List<PaymentMethodModel>();
        var paymentMethods = _paymentService.LoadAllPaymentMethods();
        foreach (var paymentMethod in paymentMethods)
        {
            var tmp = await paymentMethod.ToModel();
            tmp.IsActive = paymentMethod.IsPaymentMethodActive(_paymentSettings);
            var pluginInfo =
                PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == paymentMethod.SystemName);
            if (pluginInfo != null)
            {
                var plugin = pluginInfo.Instance<IPlugin>(_serviceProvider);
                if (plugin != null)
                {
                    tmp.ConfigurationUrl = plugin.ConfigurationUrl();
                    tmp.LogoUrl = pluginInfo.GetLogoUrl(_workContext);
                }
            }

            paymentMethodsModel.Add(tmp);
        }

        paymentMethodsModel = paymentMethodsModel.ToList();
        var gridModel = new DataSourceResult {
            Data = paymentMethodsModel,
            Total = paymentMethodsModel.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> MethodUpdate(PaymentMethodModel model)
    {
        var storeScope = await GetActiveStore();
        var _paymentSettings = _settingService.LoadSetting<PaymentSettings>(storeScope);

        var pm = _paymentService.LoadPaymentMethodBySystemName(model.SystemName);
        if (pm.IsPaymentMethodActive(_paymentSettings))
        {
            if (!model.IsActive)
            {
                //mark as disabled
                _paymentSettings.ActivePaymentProviderSystemNames.Remove(pm.SystemName);
                await _settingService.SaveSetting(_paymentSettings, storeScope);
            }
        }
        else
        {
            if (model.IsActive)
            {
                //mark as active
                _paymentSettings.ActivePaymentProviderSystemNames.Add(pm.SystemName);
                await _settingService.SaveSetting(_paymentSettings, storeScope);
            }
        }

        return new JsonResult("");
    }

    public async Task<IActionResult> ConfigureMethod(string systemName)
    {
        var pm = _paymentService.LoadPaymentMethodBySystemName(systemName);
        if (pm == null)
            //No payment method found with the specified id
            return RedirectToAction("Methods");

        var model = await pm.ToModel();
        //TODO
        /*
        model.LogoUrl = "";
        */
        model.ConfigurationUrl = pm.ConfigurationUrl;
        return View(model);
    }

    public async Task<IActionResult> MethodRestrictions()
    {
        var model = new PaymentMethodRestrictionModel();
        var paymentMethods = _paymentService.LoadAllPaymentMethods();
        var countries = await _countryService.GetAllCountries(showHidden: true);
        var shippings = await _shippingMethodService.GetAllShippingMethods();

        foreach (var pm in paymentMethods) model.AvailablePaymentMethods.Add(await pm.ToModel());
        foreach (var c in countries) model.AvailableCountries.Add(c.ToModel());
        foreach (var s in shippings)
            model.AvailableShippingMethods.Add(new ShippingMethodModel {
                Id = s.Id,
                Name = s.Name
            });

        foreach (var pm in paymentMethods)
        {
            var restictedCountries = _paymentService.GetRestrictedCountryIds(pm);
            foreach (var c in countries)
            {
                var resticted = restictedCountries.Contains(c.Id);
                if (!model.Resticted.ContainsKey(pm.SystemName))
                    model.Resticted[pm.SystemName] = new Dictionary<string, bool>();
                model.Resticted[pm.SystemName][c.Id] = resticted;
            }

            var restictedShipping = _paymentService.GetRestrictedShippingIds(pm);
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
    public async Task<IActionResult> MethodRestrictionsSave(IDictionary<string, string[]> model)
    {
        var paymentMethods = _paymentService.LoadAllPaymentMethods();
        var countries = await _countryService.GetAllCountries(showHidden: true);
        var shippings = await _shippingMethodService.GetAllShippingMethods();

        foreach (var pm in paymentMethods)
        {
            if (model.TryGetValue($"restrict_{pm.SystemName.Replace(".", "")}", out var countryIds))
            {
                var countryIdsToRestrict = countryIds.ToList();
                var newCountryIds =
                    (from c in countries where countryIdsToRestrict.Contains(c.Id) select c.Id).ToList();
                await _paymentService.SaveRestrictedCountryIds(pm, newCountryIds);
            }
            else
            {
                await _paymentService.SaveRestrictedCountryIds(pm, new List<string>());
            }

            if (model.TryGetValue($"restrictship_{pm.SystemName.Replace(".", "")}", out var shipIds))
            {
                var shipIdsToRestrict = shipIds.ToList();
                var newShipIds = (from s in shippings where shipIdsToRestrict.Contains(s.Name) select s.Name).ToList();
                await _paymentService.SaveRestrictedShippingIds(pm, newShipIds);
            }
            else
            {
                await _paymentService.SaveRestrictedShippingIds(pm, new List<string>());
            }
        }

        Success(_translationService.GetResource("Admin.Configuration.Payment.MethodRestrictions.Updated"));
        //selected tab
        await SaveSelectedTabIndex();
        return RedirectToAction("MethodRestrictions");
    }

    #region Shipping Settings

    public async Task<IActionResult> Settings()
    {
        var storeScope = await GetActiveStore();

        var paymentSettings = _settingService.LoadSetting<PaymentSettings>(storeScope);
        var model = paymentSettings.ToModel();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(PaymentSettingsModel model)
    {
        var storeScope = await GetActiveStore();

        var paymentSettings = _settingService.LoadSetting<PaymentSettings>(storeScope);
        paymentSettings = model.ToEntity(paymentSettings);

        await _settingService.SaveSetting(paymentSettings, storeScope);

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Settings");
    }

    #endregion

    #endregion
}