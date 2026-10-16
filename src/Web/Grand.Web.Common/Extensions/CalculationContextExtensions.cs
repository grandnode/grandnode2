using Grand.Business.Core.Utilities.Checkout;
using Grand.Infrastructure;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Builds the calculation context of the current request
/// </summary>
public static class CalculationContextExtensions
{
    /// <summary>
    ///     Gets the calculation context of the current request: its customer, store, working currency and tax display
    /// </summary>
    public static CalculationContext CalculationContext(this IContextAccessor contextAccessor)
    {
        return new CalculationContext(contextAccessor.WorkContext.CurrentCustomer,
            contextAccessor.StoreContext.CurrentStore, contextAccessor.WorkContext.WorkingCurrency,
            contextAccessor.WorkContext.TaxDisplayType);
    }
}
