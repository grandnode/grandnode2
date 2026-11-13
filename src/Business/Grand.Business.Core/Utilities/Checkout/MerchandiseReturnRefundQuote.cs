namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     What the refund of a merchandise return comes to now, in the currency of its order
/// </summary>
public class MerchandiseReturnRefundQuote
{
    /// <summary>
    ///     Why the return cannot be refunded now, as a key of MerchandiseReturnErrors; null when it can
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    ///     What was paid for the items of the return: their price with the item discounts and the tax
    /// </summary>
    public double ItemsAmount { get; set; }

    /// <summary>
    ///     What the customer pays to ship the return, deducted from the items
    /// </summary>
    public double ReturnShippingCost { get; set; }

    /// <summary>
    ///     The items less the return shipping cost, never below zero
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    ///     The most that can be refunded: the amount, or what is left to refund of the order's payment when that is less
    /// </summary>
    public double MaxAmount { get; set; }

    public string CurrencyCode { get; set; }

    /// <summary>
    ///     Whether the payment provider gives the money back itself
    /// </summary>
    public bool CanRefundOnline { get; set; }

    /// <summary>
    ///     Whether the refund can be recorded as given back outside the payment provider
    /// </summary>
    public bool CanRefundOffline { get; set; }
}
