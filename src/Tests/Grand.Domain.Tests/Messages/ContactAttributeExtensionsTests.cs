using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Messages;

[TestClass]
public class ContactAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_NullContactAttribute_ReturnFalse()
    {
        ContactAttribute contactAttribute = null;
        Assert.IsFalse(contactAttribute.ShouldHaveValues());
    }

    [TestMethod]
    public void ShouldHaveValues__ReturnFalse()
    {
        var contactAttribute1 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.TextBox };
        var contactAttribute2 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.MultilineTextbox };
        var contactAttribute3 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.Datepicker };
        var contactAttribute4 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.FileUpload };
        Assert.IsFalse(contactAttribute1.ShouldHaveValues());
        Assert.IsFalse(contactAttribute2.ShouldHaveValues());
        Assert.IsFalse(contactAttribute3.ShouldHaveValues());
        Assert.IsFalse(contactAttribute4.ShouldHaveValues());
    }


    [TestMethod]
    public void ShouldHaveValues__ReturnTrue()
    {
        var contactAttribute1 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.DropdownList };
        var contactAttribute2 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.ImageSquares };
        var contactAttribute3 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.RadioList };
        var contactAttribute4 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.ReadonlyCheckboxes };
        Assert.IsTrue(contactAttribute1.ShouldHaveValues());
        Assert.IsTrue(contactAttribute2.ShouldHaveValues());
        Assert.IsTrue(contactAttribute3.ShouldHaveValues());
        Assert.IsTrue(contactAttribute4.ShouldHaveValues());
    }

    [TestMethod]
    public void CanBeUsedAsConditionTest__ReturnFalse()
    {
        var contactAttribute1 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.TextBox };
        var contactAttribute2 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.MultilineTextbox };
        var contactAttribute3 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.Datepicker };
        var contactAttribute4 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.FileUpload };
        Assert.IsFalse(contactAttribute1.CanBeUsedAsCondition());
        Assert.IsFalse(contactAttribute2.CanBeUsedAsCondition());
        Assert.IsFalse(contactAttribute3.CanBeUsedAsCondition());
        Assert.IsFalse(contactAttribute4.CanBeUsedAsCondition());
    }

    [TestMethod]
    public void CanBeUsedAsConditionTest__ReturnTruee()
    {
        var contactAttribute1 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.DropdownList };
        var contactAttribute2 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.Checkboxes };
        var contactAttribute3 = new ContactAttribute { AttributeControlTypeId = (int)AttributeControlType.RadioList };
        var contactAttribute4 = new ContactAttribute
            { AttributeControlTypeId = (int)AttributeControlType.ColorSquares };
        Assert.IsTrue(contactAttribute1.CanBeUsedAsCondition());
        Assert.IsTrue(contactAttribute2.CanBeUsedAsCondition());
        Assert.IsTrue(contactAttribute3.CanBeUsedAsCondition());
        Assert.IsTrue(contactAttribute4.CanBeUsedAsCondition());
    }
}