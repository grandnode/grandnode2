using Grand.Domain.Catalog;

namespace Grand.Domain.Orders;

/// <summary>
///     Extensions
/// </summary>
public static class CheckoutAttributeExtensions
{
    /// <summary>
    ///     Gets a value indicating whether - checkout attribute should have values
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    /// <returns>Result</returns>
    public static bool ShouldHaveValues(this CheckoutAttribute checkoutAttribute)
    {
        if (checkoutAttribute == null)
            return false;

        if (checkoutAttribute.AttributeControlTypeId == AttributeControlType.TextBox ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.Datepicker ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.FileUpload)
            return false;

        return true;
    }

    /// <summary>
    ///     A value indicating whether this checkout attribute can be used
    /// </summary>
    /// <param name="checkoutAttribute">Checkout attribute</param>
    /// <returns>Result</returns>
    public static bool CanBeUsedAsCondition(this CheckoutAttribute checkoutAttribute)
    {
        if (checkoutAttribute == null)
            return false;

        if (checkoutAttribute.AttributeControlTypeId == AttributeControlType.ReadonlyCheckboxes ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.TextBox ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.Datepicker ||
            checkoutAttribute.AttributeControlTypeId == AttributeControlType.FileUpload)
            return false;

        return true;
    }
}