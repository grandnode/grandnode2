using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

/// <summary>
///     Product attribute formatter interface
/// </summary>
public interface IProductAttributeFormatter
{
    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="context">Customer, store, currency and tax display the prices are rendered for (read only when renderPrices is true)</param>
    /// <param name="language">Language of the attribute names</param>
    /// <param name="host">Store address used for file download links (read only when allowHyperlinks is true)</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="renderProductAttributes">A value indicating whether to render product attributes</param>
    /// <param name="renderGiftVoucherAttributes">A value indicating whether to render gift voucher attributes</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <param name="showInAdmin">Show in admin</param>
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes,
        CalculationContext context, Language language, DomainHost host,
        string separator = "<br />", bool htmlEncode = true, bool renderPrices = true,
        bool renderProductAttributes = true, bool renderGiftVoucherAttributes = true,
        bool allowHyperlinks = true, bool showInAdmin = false);
}
