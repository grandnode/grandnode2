using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Payments
{
    public partial class PaymentSettingsModel : BaseModel
    {

        [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.AllowRePostingPayments")]
        public bool AllowRePostingPayments { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.SkipPaymentIfOnlyOne")]
        public bool SkipPaymentIfOnlyOne { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.ShowPaymentIfCartIsZero")]
        public bool ShowPaymentIfCartIsZero { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.ShowPaymentDescriptions")]
        public bool ShowPaymentDescriptions { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.SkipPaymentInfo")]
        public bool SkipPaymentInfo { get; set; }
    }
}