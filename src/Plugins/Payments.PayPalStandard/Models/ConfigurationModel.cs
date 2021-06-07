using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Payments.PayPalStandard.Models
{
    public class ConfigurationModel : BaseModel
    {
        public string StoreScope { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.BusinessEmail")]
        public string BusinessEmail { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.PDTToken")]
        public string PdtToken { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.PDTValidateOrderTotal")]
        public bool PdtValidateOrderTotal { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.AdditionalFee")]
        public double AdditionalFee { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.PayPalStandard.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }



    }
}