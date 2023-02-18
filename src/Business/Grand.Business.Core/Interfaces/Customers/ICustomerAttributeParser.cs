using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;

namespace Grand.Business.Core.Interfaces.Customers
{
    /// <summary>
    /// Customer attribute parser interface
    /// </summary>
    public interface ICustomerAttributeParser
    {
        /// <summary>
        /// Gets selected customer attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Selected customer attributes</returns>
        Task<IList<CustomerAttribute>> ParseCustomerAttributes(IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Get customer attribute values
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Customer attribute values</returns>
        Task<IList<CustomerAttributeValue>> ParseCustomerAttributeValues(IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="ca">Customer attribute</param>
        /// <param name="value">Value</param>
        /// <returns>Attributes</returns>
        IList<CustomAttribute> AddCustomerAttribute(IList<CustomAttribute> customAttributes, CustomerAttribute ca, string value);

        /// <summary>
        /// Validates customer attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Warnings</returns>
        Task<IList<string>> GetAttributeWarnings(IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="separator">Separator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <returns>Attributes</returns>
        Task<string> FormatAttributes(Language language, IList<CustomAttribute> customAttributes, string separator = "<br />", bool htmlEncode = true);

    }
}
