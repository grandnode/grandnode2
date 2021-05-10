using Grand.Domain.Catalog;
using Grand.Domain.Customers;

namespace Grand.Business.Customers.Extensions
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class CustomerAttributeExtensions
    {
        /// <summary>
        /// A value indicating whether this customer attribute should have values
        /// </summary>
        /// <param name="customerAttribute">Customer attribute</param>
        /// <returns>Result</returns>
        public static bool ShouldHaveValues(this CustomerAttribute customerAttribute)
        {
            if (customerAttribute == null)
                return false;

            if (customerAttribute.AttributeControlTypeId == AttributeControlType.TextBox ||
                customerAttribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
                customerAttribute.AttributeControlTypeId == AttributeControlType.Datepicker ||
                customerAttribute.AttributeControlTypeId == AttributeControlType.FileUpload)
                return false;

            //other attribute controle types support values
            return true;
        }
    }
}
