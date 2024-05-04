using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Marketing.Contacts;

/// <summary>
///     Contact attribute parser interface
/// </summary>
public interface IContactAttributeParser
{
    /// <summary>
    ///     Gets selected contact attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Selected contact attributes</returns>
    Task<IList<ContactAttribute>> ParseContactAttributes(IList<CustomAttribute> customAttributes);

    /// <summary>
    ///     Get contact attribute values
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Contact attribute values</returns>
    Task<IList<ContactAttributeValue>> ParseContactAttributeValues(IList<CustomAttribute> customAttributes);

    /// <summary>
    ///     Adds an attribute
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="ca">Contact attribute</param>
    /// <param name="value">Value</param>
    /// <returns>Attributes</returns>
    IList<CustomAttribute> AddContactAttribute(IList<CustomAttribute> customAttributes, ContactAttribute ca,
        string value);

    /// <summary>
    ///     Check whether condition of some attribute is met (if specified). Return "null" if not condition is specified
    /// </summary>
    /// <param name="attribute">Contact attribute</param>
    /// <param name="customAttributes">Selected attributes</param>
    /// <returns>Result</returns>
    Task<bool?> IsConditionMet(ContactAttribute attribute, IList<CustomAttribute> customAttributes);

    /// <summary>
    ///     Remove an attribute
    /// </summary>
    /// <param name="customAttributes">Attributes in XML format</param>
    /// <param name="attribute">contact attribute</param>
    /// <returns>Updated result (XML format)</returns>
    IList<CustomAttribute> RemoveContactAttribute(IList<CustomAttribute> customAttributes, ContactAttribute attribute);

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="language">Language</param>
    /// <param name="customAttributes">Attributes </param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <returns>Attributes</returns>
    Task<string> FormatAttributes(
        Language language,
        IList<CustomAttribute> customAttributes,
        Customer customer,
        string separator = "<br />",
        bool htmlEncode = true,
        bool allowHyperlinks = true);

    /// <summary>
    ///     Get parse contact attributes
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<IList<CustomAttribute>> GetParseContactAttributes(IList<CustomAttribute> model);
}