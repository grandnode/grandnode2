using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

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
    /// <param name="context">Customer, store, currency and tax display the prices are rendered for</param>
    /// <param name="language">Language of the attribute names</param>
    /// <param name="host">Store address used for file download links (read only when allowHyperlinks is true)</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(IList<CustomAttribute> customAttributes,
        CalculationContext context,
        Language language,
        DomainHost host,
        string separator = "<br />",
        bool htmlEncode = true,
        bool renderPrices = true,
        bool allowHyperlinks = true);
}