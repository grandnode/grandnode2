using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;

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
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes);

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="renderProductAttributes">A value indicating whether to render product attributes</param>
    /// <param name="renderGiftVoucherAttributes">A value indicating whether to render gift voucher attributes</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <param name="showInAdmin">Show in admin</param>
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes,
        Customer customer, string separator = "<br />", bool htmlEncode = true, bool renderPrices = true,
        bool renderProductAttributes = true, bool renderGiftVoucherAttributes = true,
        bool allowHyperlinks = true, bool showInAdmin = false);
}