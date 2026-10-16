using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Domain.Tax;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Who and where a checkout calculation is for: the customer whose cart, discounts and loyalty points apply,
///     the store, the currency amounts are rounded and converted in, and whether prices include tax
/// </summary>
public sealed record CalculationContext(Customer Customer, Store Store, Currency Currency, TaxDisplayType TaxDisplayType)
{
    /// <summary>
    ///     Gets a value indicating whether prices are calculated including tax
    /// </summary>
    public bool IncludingTax => TaxDisplayType == TaxDisplayType.IncludingTax;
}
