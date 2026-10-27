using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Payments;

public class PaymentSettingsModel : BaseModel
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

    /// <summary>
    ///     Payment methods offered for orders that wait for an approval; global, not per store
    /// </summary>
    [GrandResourceDisplayName("Admin.Configuration.Payment.Settings.OrderApprovalPaymentMethods")]
    public IList<string> OrderApprovalPaymentMethodSystemNames { get; set; } = new List<string>();

    public IList<SelectListItem> AvailableOrderApprovalPaymentMethods { get; set; } = new List<SelectListItem>();
}
