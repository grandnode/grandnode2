using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Marketing.Tests.Extensions;

[TestClass]
public class ContactAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_ReturnExpentedResult()
    {
        var ca = new ContactAttribute { AttributeControlType = AttributeControlType.TextBox };
        var ca2 = new ContactAttribute { AttributeControlType = AttributeControlType.MultilineTextbox };
        var ca3 = new ContactAttribute { AttributeControlType = AttributeControlType.Datepicker };
        var ca4 = new ContactAttribute { AttributeControlType = AttributeControlType.FileUpload };
        var ca5 = new ContactAttribute { AttributeControlType = AttributeControlType.DropdownList };
        ContactAttribute ca6 = null;
        Assert.IsFalse(ca.ShouldHaveValues());
        Assert.IsFalse(ca6.ShouldHaveValues());
        Assert.IsFalse(ca2.ShouldHaveValues());
        Assert.IsFalse(ca4.ShouldHaveValues());
        Assert.IsFalse(ca3.ShouldHaveValues());
        Assert.IsTrue(ca5.ShouldHaveValues());
    }

    [TestMethod]
    public void CanBeUsedAsCondition_ReturnExpentedResult()
    {
        var ca = new ContactAttribute { AttributeControlType = AttributeControlType.TextBox };
        var ca2 = new ContactAttribute { AttributeControlType = AttributeControlType.MultilineTextbox };
        var ca3 = new ContactAttribute { AttributeControlType = AttributeControlType.Datepicker };
        var ca4 = new ContactAttribute { AttributeControlType = AttributeControlType.FileUpload };
        var ca5 = new ContactAttribute { AttributeControlType = AttributeControlType.DropdownList };
        ContactAttribute ca6 = null;
        Assert.IsFalse(ca.CanBeUsedAsCondition());
        Assert.IsFalse(ca6.CanBeUsedAsCondition());
        Assert.IsFalse(ca2.CanBeUsedAsCondition());
        Assert.IsFalse(ca4.CanBeUsedAsCondition());
        Assert.IsFalse(ca3.CanBeUsedAsCondition());
        Assert.IsTrue(ca5.CanBeUsedAsCondition());
    }
}