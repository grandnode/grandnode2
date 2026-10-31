using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     How the buyer's purchase order number is cleaned and checked before it is stored on the order
/// </summary>
public static class PurchaseOrderNumberRules
{
    /// <summary>
    ///     Maximum length of a purchase order number
    /// </summary>
    public const int MaxLength = 50;

    /// <summary>
    ///     Gets the number to store: trimmed and kept as typed, null when empty or when the checkout does not ask
    ///     for it
    /// </summary>
    /// <param name="value">Number entered by the buyer</param>
    /// <param name="mode">Purchase order number mode</param>
    public static string Normalize(string value, PurchaseOrderNumberMode mode)
    {
        if (mode == PurchaseOrderNumberMode.Disabled || string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    /// <summary>
    ///     Checks a normalized number
    /// </summary>
    /// <param name="normalized">Number returned by <see cref="Normalize" /></param>
    /// <param name="mode">Purchase order number mode</param>
    /// <returns>Resource key of the error; null when the number is valid</returns>
    public static string Validate(string normalized, PurchaseOrderNumberMode mode)
    {
        if (normalized == null)
            return mode == PurchaseOrderNumberMode.Required ? "Checkout.PurchaseOrderNumber.Required" : null;

        return normalized.Length > MaxLength ? "Checkout.PurchaseOrderNumber.TooLong" : null;
    }
}
