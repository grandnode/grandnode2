using Grand.Web.Common.Localization;
using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Payments.CashOnDelivery.Models
{
    public class ConfigurationModel : BaseModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
    {
        public ConfigurationModel()
        {
            Locales = new List<ConfigurationLocalizedModel>();
        }

        public string ActiveStore { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.DescriptionText")]
        public string DescriptionText { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.AdditionalFee")]
        public double AdditionalFee { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.DisplayOrder")]
        public int DisplayOrder { get; set; }


        public IList<ConfigurationLocalizedModel> Locales { get; set; }

        #region Nested class

        public partial class ConfigurationLocalizedModel : ILocalizedModelLocal
        {
            public string LanguageId { get; set; }

            [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.DescriptionText")]
            public string DescriptionText { get; set; }
        }

        #endregion
    }
}