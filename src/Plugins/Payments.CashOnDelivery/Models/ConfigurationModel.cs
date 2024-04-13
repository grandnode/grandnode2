using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Payments.CashOnDelivery.Models;

public class ConfigurationModel : BaseModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
{
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

    [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.SkipPaymentInfo")]
    public bool SkipPaymentInfo { get; set; }

    public IList<ConfigurationLocalizedModel> Locales { get; set; } = new List<ConfigurationLocalizedModel>();

    #region Nested class

    public class ConfigurationLocalizedModel : ILocalizedModelLocal
    {
        [GrandResourceDisplayName("Plugins.Payment.CashOnDelivery.DescriptionText")]
        public string DescriptionText { get; set; }

        public string LanguageId { get; set; }
    }

    #endregion
}