using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Orders;

[TestClass]
public class CheckoutAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_NullCheckoutAttribute_ReturnFalse()
    {
        CheckoutAttribute checkoutAttribute = null;
        Assert.IsFalse(checkoutAttribute.ShouldHaveValues());
    }

    [TestMethod]
    public void ShouldHaveValues__ReturnFalse()
    {
        var checkoutAttribute1 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.TextBox };
        var checkoutAttribute2 = new CheckoutAttribute
            { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var checkoutAttribute3 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.Datepicker };
        var checkoutAttribute4 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.FileUpload };
        Assert.IsFalse(checkoutAttribute1.ShouldHaveValues());
        Assert.IsFalse(checkoutAttribute2.ShouldHaveValues());
        Assert.IsFalse(checkoutAttribute3.ShouldHaveValues());
        Assert.IsFalse(checkoutAttribute4.ShouldHaveValues());
    }


    [TestMethod]
    public void ShouldHaveValues__ReturnTrue()
    {
        var checkoutAttribute1 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.DropdownList };
        var checkoutAttribute2 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.ImageSquares };
        var checkoutAttribute3 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.RadioList };
        var checkoutAttribute4 = new CheckoutAttribute
            { AttributeControlTypeId = AttributeControlType.ReadonlyCheckboxes };
        Assert.IsTrue(checkoutAttribute1.ShouldHaveValues());
        Assert.IsTrue(checkoutAttribute2.ShouldHaveValues());
        Assert.IsTrue(checkoutAttribute3.ShouldHaveValues());
        Assert.IsTrue(checkoutAttribute4.ShouldHaveValues());
    }

    [TestMethod]
    public void CanBeUsedAsConditionTest__ReturnFalse()
    {
        var checkoutAttribute1 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.TextBox };
        var checkoutAttribute2 = new CheckoutAttribute
            { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var checkoutAttribute3 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.Datepicker };
        var checkoutAttribute4 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.FileUpload };
        Assert.IsFalse(checkoutAttribute1.CanBeUsedAsCondition());
        Assert.IsFalse(checkoutAttribute2.CanBeUsedAsCondition());
        Assert.IsFalse(checkoutAttribute3.CanBeUsedAsCondition());
        Assert.IsFalse(checkoutAttribute4.CanBeUsedAsCondition());
    }

    [TestMethod]
    public void CanBeUsedAsConditionTest__ReturnTruee()
    {
        var checkoutAttribute1 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.DropdownList };
        var checkoutAttribute2 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.Checkboxes };
        var checkoutAttribute3 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.RadioList };
        var checkoutAttribute4 = new CheckoutAttribute { AttributeControlTypeId = AttributeControlType.ColorSquares };
        Assert.IsTrue(checkoutAttribute1.CanBeUsedAsCondition());
        Assert.IsTrue(checkoutAttribute2.CanBeUsedAsCondition());
        Assert.IsTrue(checkoutAttribute3.CanBeUsedAsCondition());
        Assert.IsTrue(checkoutAttribute4.CanBeUsedAsCondition());
    }
}