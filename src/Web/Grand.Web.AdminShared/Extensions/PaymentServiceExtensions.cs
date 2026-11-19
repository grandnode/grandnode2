using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Extensions;

public static class PaymentServiceExtensions
{
    /// <summary>
    ///     Get the payment methods a list can be filtered by, after an "All" item
    /// </summary>
    /// <param name="paymentService">Payment service</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="storeId">The store whose methods are listed; empty for the methods of every store</param>
    /// <returns>Select list items whose value is the method's system name</returns>
    public static async Task<IList<SelectListItem>> PaymentMethodSelectList(this IPaymentService paymentService,
        IRequestTranslationService translationService, string storeId)
    {
        var items = new List<SelectListItem> {
            new() { Text = translationService.GetResource("Admin.Common.All"), Value = " " }
        };
        foreach (var pm in await paymentService.LoadAllPaymentMethods(storeId: storeId ?? ""))
            items.Add(new SelectListItem { Text = pm.FriendlyName, Value = pm.SystemName });

        return items;
    }
}
