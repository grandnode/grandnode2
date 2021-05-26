using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Payments;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.PaymentMethods)]
    public partial class PaymentController : BaseAdminController
    {
        #region Fields

        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly ICountryService _countryService;
        private readonly IGroupService _groupService;
        private readonly IShippingMethodService _shippingMethodService;
        private readonly ITranslationService _translationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkContext _workContext;

        #endregion

        #region Constructors

        public PaymentController(IPaymentService paymentService,
            ISettingService settingService,
            ICountryService countryService,
            IGroupService groupService,
            IShippingMethodService shippingMethodService,
            ITranslationService translationService,
            IServiceProvider serviceProvider,
            IWorkContext workContext)
        {
            _paymentService = paymentService;
            _settingService = settingService;
            _countryService = countryService;
            _groupService = groupService;
            _shippingMethodService = shippingMethodService;
            _translationService = translationService;
            _serviceProvider = serviceProvider;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Methods()
        {
            var _paymentSettings = _settingService.LoadSetting<PaymentSettings>();

            var paymentMethodsModel = new List<PaymentMethodModel>();
            var paymentMethods = _paymentService.LoadAllPaymentMethods();
            foreach (var paymentMethod in paymentMethods)
            {
                var tmp = await paymentMethod.ToModel();
                tmp.IsActive = paymentMethod.IsPaymentMethodActive(_paymentSettings);
                var pluginInfo = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName == paymentMethod.SystemName);
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
            var _paymentSettings = _settingService.LoadSetting<PaymentSettings>();

            var pm = _paymentService.LoadPaymentMethodBySystemName(model.SystemName);
            if (pm.IsPaymentMethodActive(_paymentSettings))
            {
                if (!model.IsActive)
                {
                    //mark as disabled
                    _paymentSettings.ActivePaymentProviderSystemNames.Remove(pm.SystemName);
                    await _settingService.SaveSetting(_paymentSettings);
                }
            }
            else
            {
                if (model.IsActive)
                {
                    //mark as active
                    _paymentSettings.ActivePaymentProviderSystemNames.Add(pm.SystemName);
                    await _settingService.SaveSetting(_paymentSettings);
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
            model.LogoUrl = pm.PluginInfo.GetLogoUrl(_webHelper);
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

            foreach (var pm in paymentMethods)
            {
                model.AvailablePaymentMethods.Add(await pm.ToModel());
            }
            foreach (var c in countries)
            {
                model.AvailableCountries.Add(c.ToModel());
            }
            foreach (var s in shippings)
            {
                model.AvailableShippingMethods.Add(new Models.Shipping.ShippingMethodModel() {
                    Id = s.Id,
                    Name = s.Name
                });
            }

            foreach (var pm in paymentMethods)
            {
                var restictedCountries = _paymentService.GetRestrictedCountryIds(pm);
                foreach (var c in countries)
                {
                    bool resticted = restictedCountries.Contains(c.Id);
                    if (!model.Resticted.ContainsKey(pm.SystemName))
                        model.Resticted[pm.SystemName] = new Dictionary<string, bool>();
                    model.Resticted[pm.SystemName][c.Id] = resticted;
                }

                var restictedShipping = _paymentService.GetRestrictedShippingIds(pm);
                foreach (var s in shippings)
                {
                    bool resticted = restictedShipping.Contains(s.Name);
                    if (!model.RestictedShipping.ContainsKey(pm.SystemName))
                        model.RestictedShipping[pm.SystemName] = new Dictionary<string, bool>();
                    model.RestictedShipping[pm.SystemName][s.Name] = resticted;
                }

            }

            return View(model);
        }

        [HttpPost, ActionName("MethodRestrictions")]
        [RequestFormLimits(ValueCountLimit = 2048)]
        public async Task<IActionResult> MethodRestrictionsSave(IFormCollection form)
        {
            var paymentMethods = _paymentService.LoadAllPaymentMethods();
            var countries = await _countryService.GetAllCountries(showHidden: true);
            var shippings = await _shippingMethodService.GetAllShippingMethods();

            foreach (var pm in paymentMethods)
            {
                string formKey = "restrict_" + pm.SystemName;
                var countryIdsToRestrict = (form[formKey].ToString() != null ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>())
                    .Select(x => x).ToList();

                var newCountryIds = new List<string>();
                foreach (var c in countries)
                {
                    if (countryIdsToRestrict.Contains(c.Id))
                    {
                        newCountryIds.Add(c.Id);
                    }
                }
                await _paymentService.SaveRestictedCountryIds(pm, newCountryIds);

                formKey = "restrictship_" + pm.SystemName;
                var shipIdsToRestrict = (form[formKey].ToString() != null ? form[formKey].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>())
                    .Select(x => x).ToList();

                var newShipIds = new List<string>();
                foreach (var s in shippings)
                {
                    if (shipIdsToRestrict.Contains(s.Name))
                    {
                        newShipIds.Add(s.Name);
                    }
                }
                await _paymentService.SaveRestictedShippingIds(pm, newShipIds);
            }

            Success(_translationService.GetResource("Admin.Configuration.Payment.MethodRestrictions.Updated"));
            //selected tab
            await SaveSelectedTabIndex();
            return RedirectToAction("MethodRestrictions");
        }

        #region Shipping Settings

        public IActionResult Settings()
        {
            var paymentSettings = _settingService.LoadSetting<PaymentSettings>();
            var model = paymentSettings.ToModel();

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Settings(PaymentSettingsModel model,
            [FromServices] ICustomerActivityService customerActivityService)
        {
            var paymentSettings = _settingService.LoadSetting<PaymentSettings>();
            paymentSettings = model.ToEntity(paymentSettings);

            await _settingService.SaveSetting(paymentSettings);

            //activity log
            await customerActivityService.InsertActivity("EditSettings", "", _translationService.GetResource("ActivityLog.EditSettings"));

            Success(_translationService.GetResource("Admin.Configuration.Updated"));
            return RedirectToAction("Settings");
        }

        #endregion

        #endregion
    }
}
