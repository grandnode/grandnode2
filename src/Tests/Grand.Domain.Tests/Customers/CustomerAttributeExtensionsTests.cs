using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Customers;

[TestClass]
public class CustomerAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_NullCustomerAttribute_ReturnFalse()
    {
        CustomerAttribute customerAttribute = null;
        Assert.IsFalse(customerAttribute.ShouldHaveValues());
    }

    [TestMethod]
    public void ShouldHaveValues__ReturnFalse()
    {
        var customerAttribute1 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.TextBox };
        var customerAttribute2 = new CustomerAttribute
            { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var customerAttribute3 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.Datepicker };
        var customerAttribute4 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.FileUpload };
        Assert.IsFalse(customerAttribute1.ShouldHaveValues());
        Assert.IsFalse(customerAttribute2.ShouldHaveValues());
        Assert.IsFalse(customerAttribute3.ShouldHaveValues());
        Assert.IsFalse(customerAttribute4.ShouldHaveValues());
    }


    [TestMethod]
    public void ShouldHaveValues__ReturnTrue()
    {
        var addressAttribute1 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.DropdownList };
        var addressAttribute2 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.ImageSquares };
        var addressAttribute3 = new CustomerAttribute { AttributeControlTypeId = AttributeControlType.RadioList };
        var addressAttribute4 = new CustomerAttribute
            { AttributeControlTypeId = AttributeControlType.ReadonlyCheckboxes };
        Assert.IsTrue(addressAttribute1.ShouldHaveValues());
        Assert.IsTrue(addressAttribute2.ShouldHaveValues());
        Assert.IsTrue(addressAttribute3.ShouldHaveValues());
        Assert.IsTrue(addressAttribute4.ShouldHaveValues());
    }
}