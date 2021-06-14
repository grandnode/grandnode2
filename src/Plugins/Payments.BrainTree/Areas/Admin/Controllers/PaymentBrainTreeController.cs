using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.BrainTree.Models;
using System.Threading.Tasks;

namespace Payments.BrainTree.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.PaymentMethods)]
    public class PaymentBrainTreeController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        #endregion

        #region Ctor

        public PaymentBrainTreeController(ISettingService settingService,
            ITranslationService translationService,
            BrainTreePaymentSettings brainTreePaymentSettings)
        {
            _settingService = settingService;
            _translationService = translationService;
            _brainTreePaymentSettings = brainTreePaymentSettings;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                Use3DS = _brainTreePaymentSettings.Use3DS,
                UseSandBox = _brainTreePaymentSettings.UseSandBox,
                PublicKey = _brainTreePaymentSettings.PublicKey,
                PrivateKey = _brainTreePaymentSettings.PrivateKey,
                MerchantId = _brainTreePaymentSettings.MerchantId,
                AdditionalFee = _brainTreePaymentSettings.AdditionalFee,
                AdditionalFeePercentage = _brainTreePaymentSettings.AdditionalFeePercentage,
                DisplayOrder = _brainTreePaymentSettings.DisplayOrder
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _brainTreePaymentSettings.Use3DS = model.Use3DS;
            _brainTreePaymentSettings.UseSandBox = model.UseSandBox;
            _brainTreePaymentSettings.PublicKey = model.PublicKey;
            _brainTreePaymentSettings.PrivateKey = model.PrivateKey;
            _brainTreePaymentSettings.MerchantId = model.MerchantId;
            _brainTreePaymentSettings.AdditionalFee = model.AdditionalFee;
            _brainTreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            _brainTreePaymentSettings.DisplayOrder = model.DisplayOrder;

            await _settingService.SaveSetting(_brainTreePaymentSettings);

            Success(_translationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}