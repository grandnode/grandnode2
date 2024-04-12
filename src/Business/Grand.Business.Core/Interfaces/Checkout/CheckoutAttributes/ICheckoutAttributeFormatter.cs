using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;

/// <summary>
///     Checkout attribute helper
/// </summary>
public interface ICheckoutAttributeFormatter
{
    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(IList<CustomAttribute> customAttributes,
        Customer customer,
        string separator = "<br />",
        bool htmlEncode = true,
        bool renderPrices = true,
        bool allowHyperlinks = true);
}