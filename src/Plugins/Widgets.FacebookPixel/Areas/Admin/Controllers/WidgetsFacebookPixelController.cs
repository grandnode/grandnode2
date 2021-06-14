using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Widgets.FacebookPixel.Models;

namespace Widgets.FacebookPixel.Controllers
{
    [Area("Admin")]
    [AuthorizeAdmin]
    [PermissionAuthorize(PermissionSystemName.Widgets)]
    public class WidgetsFacebookPixelController : BasePluginController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;

        public WidgetsFacebookPixelController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ITranslationService translationService)
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _translationService = translationService;
        }
        protected virtual async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
        {
            var stores = await storeService.GetAllStores();
            if (stores.Count < 2)
                return stores.FirstOrDefault().Id;

            var storeId = workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration);
            var store = await storeService.GetStoreById(storeId);

            return store != null ? store.Id : "";
        }
        [AuthorizeAdmin]
        public async Task<IActionResult> Configure()
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var facebookPixelSettings = _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
            var model = new ConfigurationModel();
            model.PixelId = facebookPixelSettings.PixelId;
            model.PixelScript = facebookPixelSettings.PixelScript;
            model.AddToCartScript = facebookPixelSettings.AddToCartScript;
            model.DetailsOrderScript = facebookPixelSettings.DetailsOrderScript;
            model.AllowToDisableConsentCookie = facebookPixelSettings.AllowToDisableConsentCookie;
            model.ConsentName = facebookPixelSettings.ConsentName;
            model.ConsentDescription = facebookPixelSettings.ConsentDescription;
            model.ConsentDefaultState = facebookPixelSettings.ConsentDefaultState;

            return View(model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            //load settings for a chosen store scope
            var storeScope = await GetActiveStore(_storeService, _workContext);
            var facebookPixelSettings = _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
            facebookPixelSettings.PixelId = model.PixelId;
            facebookPixelSettings.PixelScript = model.PixelScript;
            facebookPixelSettings.AddToCartScript = model.AddToCartScript;
            facebookPixelSettings.DetailsOrderScript = model.DetailsOrderScript;
            facebookPixelSettings.AllowToDisableConsentCookie = model.AllowToDisableConsentCookie;
            facebookPixelSettings.ConsentName = model.ConsentName;
            facebookPixelSettings.ConsentDescription = model.ConsentDescription;
            facebookPixelSettings.ConsentDefaultState = model.ConsentDefaultState;

            await _settingService.SaveSetting(facebookPixelSettings, storeScope);

            //now clear settings cache
            await _settingService.ClearCache();
            Success(_translationService.GetResource("Admin.Plugins.Saved"));
            return await Configure();
        }

    }
}