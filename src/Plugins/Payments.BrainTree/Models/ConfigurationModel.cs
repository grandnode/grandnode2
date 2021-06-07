using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Payments.BrainTree.Models
{
    public class ConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.Use3DS")]
        public bool Use3DS { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.UseSandbox")]
        public bool UseSandBox { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.MerchantId")]
        public string MerchantId { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.PublicKey")]
        public string PublicKey { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.PrivateKey")]
        public string PrivateKey { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.AdditionalFee")]
        public double AdditionalFee { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [GrandResourceDisplayName("Plugins.Payments.BrainTree.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}