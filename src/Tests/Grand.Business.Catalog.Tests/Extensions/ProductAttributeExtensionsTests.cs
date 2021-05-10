using Grand.Business.Catalog.Extensions;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Extensions
{
    [TestClass()]
    public class ProductAttributeExtensionsTests
    {

        [TestMethod()]
        public void ShouldHaveValues_ReturnExpentedResult()
        {
            var pam = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.TextBox };
            var pam2 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.MultilineTextbox};
            var pam3 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.Datepicker };
            var pam4 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.FileUpload };
            var pam5 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.DropdownList };
            ProductAttributeMapping pam6 = null;
            Assert.AreEqual(false, pam.ShouldHaveValues());
            Assert.AreEqual(false, pam2.ShouldHaveValues());
            Assert.AreEqual(false, pam3.ShouldHaveValues());
            Assert.AreEqual(false, pam4.ShouldHaveValues());
            Assert.AreEqual(false, pam6.ShouldHaveValues());
            Assert.AreEqual(true, pam5.ShouldHaveValues());
        }


        [TestMethod()]
        public void ValidationRulesAllowed_ReturnExpentedResult()
        {
            var pam = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.TextBox };
            var pam2 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
            var pam3 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.Datepicker };
            var pam4 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.FileUpload };
            var pam5 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.DropdownList };
            ProductAttributeMapping pam6 = null;
            Assert.AreEqual(true, pam.ValidationRulesAllowed());
            Assert.AreEqual(true, pam2.ValidationRulesAllowed());
            Assert.AreEqual(true, pam4.ValidationRulesAllowed());
            Assert.AreEqual(false, pam3.ValidationRulesAllowed());
            Assert.AreEqual(false, pam6.ValidationRulesAllowed());
            Assert.AreEqual(false, pam5.ValidationRulesAllowed());
        }

        [TestMethod()]
        public void CanBeUsedAsCondition_ReturnExpentedResult()
        {
            var pam = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.TextBox };
            var pam2 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
            var pam3 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.Datepicker };
            var pam4 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.FileUpload };
            var pam5 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.DropdownList };
            ProductAttributeMapping pam6 = null;
            Assert.AreEqual(false, pam.CanBeUsedAsCondition());
            Assert.AreEqual(false, pam2.CanBeUsedAsCondition());
            Assert.AreEqual(false, pam4.CanBeUsedAsCondition());
            Assert.AreEqual(false, pam3.CanBeUsedAsCondition());
            Assert.AreEqual(false, pam6.CanBeUsedAsCondition());
            Assert.AreEqual(true, pam5.CanBeUsedAsCondition());
        }

        [TestMethod()]
        public void IsNonCombinable_ReturnExpentedResult()
        {
            var pam = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.TextBox };
            var pam2 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.MultilineTextbox };
            var pam3 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.Datepicker };
            var pam4 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.FileUpload };
            var pam5 = new ProductAttributeMapping() { AttributeControlTypeId = AttributeControlType.DropdownList };
            ProductAttributeMapping pam6 = null;
            var pam7 = new ProductAttributeMapping() {Combination=true};
            Assert.AreEqual(true, pam.IsNonCombinable());
            Assert.AreEqual(true, pam2.IsNonCombinable());
            Assert.AreEqual(true, pam4.IsNonCombinable());
            Assert.AreEqual(true, pam3.IsNonCombinable());
            Assert.AreEqual(false, pam6.IsNonCombinable());
            Assert.AreEqual(true, pam5.IsNonCombinable());
            Assert.AreEqual(false, pam7.IsNonCombinable());
        }
    }
}
