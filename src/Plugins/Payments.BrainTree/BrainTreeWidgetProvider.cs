using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Payments.BrainTree
{
    public class BrainTreeWidgetProvider : IWidgetProvider
    {
        private readonly ITranslationService _translationService;
        private readonly BrainTreePaymentSettings _brainTreePaymentSettings;

        public BrainTreeWidgetProvider(ITranslationService translationService, BrainTreePaymentSettings brainTreePaymentSettings)
        {
            _translationService = translationService;
            _brainTreePaymentSettings = brainTreePaymentSettings;
        }

        public string ConfigurationUrl => BrainTreeDefaults.ConfigurationUrl;

        public string SystemName => BrainTreeDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(BrainTreeDefaults.FriendlyName);

        public int Priority => _brainTreePaymentSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        public async Task<IList<string>> GetWidgetZones()
        {
            return await Task.FromResult(new string[] { "checkout_payment_info_top" });
        }

        public Task<string> GetPublicViewComponentName(string widgetZone)
        {
            return Task.FromResult("PaymentBrainTreeScripts");
        }
    }
}
