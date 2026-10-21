namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     Shopping cart tax total
/// </summary>
/// <param name="TaxTotal">Tax total</param>
/// <param name="TaxRates">Tax amount per tax rate</param>
public sealed record ShoppingCartTaxTotal(double TaxTotal, SortedDictionary<double, double> TaxRates);
