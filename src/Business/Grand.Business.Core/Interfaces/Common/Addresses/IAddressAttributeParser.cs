using Grand.Domain.Common;
using Grand.Domain.Localization;

namespace Grand.Business.Core.Interfaces.Common.Addresses
{
    /// <summary>
    /// Address attribute parser interface
    /// </summary>
    public interface IAddressAttributeParser
    {
        /// <summary>
        /// Gets selected address attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Selected address attributes</returns>
        Task<IList<AddressAttribute>> ParseAddressAttributes(IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Get address attribute values
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Address attribute values</returns>
        Task<IList<AddressAttributeValue>> ParseAddressAttributeValues(IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="attribute">Address attribute</param>
        /// <param name="value">Value</param>
        /// <returns>Attributes</returns>
        IList<CustomAttribute> AddAddressAttribute(IList<CustomAttribute> customAttributes, AddressAttribute attribute, string value);

        /// <summary>
        /// Validates address attributes
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
        Task<string> FormatAttributes(
            Language language,
            IList<CustomAttribute> customAttributes,
            string separator = "<br />",
            bool htmlEncode = true);
    }
}
