using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Common;

[TestClass]
public class AddressAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_NullAddressAtribute_ReturnFalse()
    {
        AddressAttribute addressAttribute = null;
        Assert.IsFalse(addressAttribute.ShouldHaveValues());
    }

    [TestMethod]
    public void ShouldHaveValues__ReturnFalse()
    {
        var addressAttribute = new AddressAttribute { AttributeControlType = AttributeControlType.TextBox };
        var addressAttribute2 = new AddressAttribute { AttributeControlType = AttributeControlType.MultilineTextbox };
        var addressAttribute3 = new AddressAttribute { AttributeControlType = AttributeControlType.Datepicker };
        var addressAttribute4 = new AddressAttribute { AttributeControlType = AttributeControlType.FileUpload };
        Assert.IsFalse(addressAttribute.ShouldHaveValues());
        Assert.IsFalse(addressAttribute2.ShouldHaveValues());
        Assert.IsFalse(addressAttribute3.ShouldHaveValues());
        Assert.IsFalse(addressAttribute4.ShouldHaveValues());
    }


    [TestMethod]
    public void ShouldHaveValues__ReturnTrue()
    {
        var addressAttribute = new AddressAttribute { AttributeControlType = AttributeControlType.DropdownList };
        var addressAttribute2 = new AddressAttribute { AttributeControlType = AttributeControlType.ImageSquares };
        var addressAttribute3 = new AddressAttribute { AttributeControlType = AttributeControlType.RadioList };
        var addressAttribute4 = new AddressAttribute { AttributeControlType = AttributeControlType.ReadonlyCheckboxes };
        Assert.IsTrue(addressAttribute.ShouldHaveValues());
        Assert.IsTrue(addressAttribute2.ShouldHaveValues());
        Assert.IsTrue(addressAttribute3.ShouldHaveValues());
        Assert.IsTrue(addressAttribute4.ShouldHaveValues());
    }
}