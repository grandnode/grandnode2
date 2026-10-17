using Grand.Business.Core.Utilities.Checkout;
using Grand.Web.Features.Models.ShoppingCart;

namespace Grand.Web.Extensions;

public static class CartCalculationRequestExtensions
{
    /// <summary>
    ///     Gets the calculation context of a cart request: its customer, store, currency and tax display
    /// </summary>
    public static CalculationContext CalculationContext(this ICartCalculationRequest request)
    {
        return new CalculationContext(request.Customer, request.Store, request.Currency, request.TaxDisplayType);
    }
}
