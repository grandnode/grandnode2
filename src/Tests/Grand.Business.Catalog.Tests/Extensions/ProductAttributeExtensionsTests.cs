using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Extensions;

[TestClass]
public class ProductAttributeExtensionsTests
{
    [TestMethod]
    public void ShouldHaveValues_ReturnExpentedResult()
    {
        var pam = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.TextBox };
        var pam2 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var pam3 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.Datepicker };
        var pam4 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.FileUpload };
        var pam5 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.DropdownList };
        ProductAttributeMapping pam6 = null;
        Assert.IsFalse(pam.ShouldHaveValues());
        Assert.IsFalse(pam2.ShouldHaveValues());
        Assert.IsFalse(pam3.ShouldHaveValues());
        Assert.IsFalse(pam4.ShouldHaveValues());
        Assert.IsFalse(pam6.ShouldHaveValues());
        Assert.IsTrue(pam5.ShouldHaveValues());
    }


    [TestMethod]
    public void ValidationRulesAllowed_ReturnExpentedResult()
    {
        var pam = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.TextBox };
        var pam2 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var pam3 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.Datepicker };
        var pam4 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.FileUpload };
        var pam5 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.DropdownList };
        ProductAttributeMapping pam6 = null;
        Assert.IsTrue(pam.ValidationRulesAllowed());
        Assert.IsTrue(pam2.ValidationRulesAllowed());
        Assert.IsTrue(pam4.ValidationRulesAllowed());
        Assert.IsFalse(pam3.ValidationRulesAllowed());
        Assert.IsFalse(pam6.ValidationRulesAllowed());
        Assert.IsFalse(pam5.ValidationRulesAllowed());
    }

    [TestMethod]
    public void CanBeUsedAsCondition_ReturnExpentedResult()
    {
        var pam = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.TextBox };
        var pam2 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var pam3 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.Datepicker };
        var pam4 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.FileUpload };
        var pam5 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.DropdownList };
        ProductAttributeMapping pam6 = null;
        Assert.IsFalse(pam.CanBeUsedAsCondition());
        Assert.IsFalse(pam2.CanBeUsedAsCondition());
        Assert.IsFalse(pam4.CanBeUsedAsCondition());
        Assert.IsFalse(pam3.CanBeUsedAsCondition());
        Assert.IsFalse(pam6.CanBeUsedAsCondition());
        Assert.IsTrue(pam5.CanBeUsedAsCondition());
    }

    [TestMethod]
    public void IsNonCombinable_ReturnExpentedResult()
    {
        var pam = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.TextBox };
        var pam2 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
        var pam3 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.Datepicker };
        var pam4 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.FileUpload };
        var pam5 = new ProductAttributeMapping { AttributeControlTypeId = AttributeControlType.DropdownList };
        ProductAttributeMapping pam6 = null;
        var pam7 = new ProductAttributeMapping { Combination = true };
        Assert.IsTrue(pam.IsNonCombinable());
        Assert.IsTrue(pam2.IsNonCombinable());
        Assert.IsTrue(pam4.IsNonCombinable());
        Assert.IsTrue(pam3.IsNonCombinable());
        Assert.IsFalse(pam6.IsNonCombinable());
        Assert.IsTrue(pam5.IsNonCombinable());
        Assert.IsFalse(pam7.IsNonCombinable());
    }
}