using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Business.Checkout.Interfaces.CheckoutAttributes
{
    /// <summary>
    /// Checkout attribute helper
    /// </summary>
    public partial interface ICheckoutAttributeFormatter
    {
        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="customer">Customer</param>
        /// <param name="serapator">Serapator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="renderPrices">A value indicating whether to render prices</param>
        /// <param name="allowHyperlinks">A value indicating whether to HTML hyperink tags could be rendered (if required)</param>
        /// <returns>Attributes</returns>
        Task<string> FormatAttributes(IList<CustomAttribute> customAttributes,
            Customer customer, 
            string serapator = "<br />", 
            bool htmlEncode = true,
            bool renderPrices = true,
            bool allowHyperlinks = true);
    }
}
