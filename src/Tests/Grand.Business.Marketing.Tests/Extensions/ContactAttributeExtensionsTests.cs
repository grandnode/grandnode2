using Grand.Business.Marketing.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Tests.Extensions
{
    [TestClass()]
    public class ContactAttributeExtensionsTests
    {
     
        [TestMethod()]
        public void ShouldHaveValues_ReturnExpentedResult()
        {
            var ca = new ContactAttribute() { AttributeControlType = AttributeControlType.TextBox };
            var ca2 = new ContactAttribute() { AttributeControlType = AttributeControlType.MultilineTextbox };
            var ca3 = new ContactAttribute() { AttributeControlType = AttributeControlType.Datepicker };
            var ca4 = new ContactAttribute() { AttributeControlType = AttributeControlType.FileUpload };
            var ca5 = new ContactAttribute() { AttributeControlType = AttributeControlType.DropdownList };
            ContactAttribute ca6 = null;
            Assert.AreEqual(false, ca.ShouldHaveValues());
            Assert.AreEqual(false, ca6.ShouldHaveValues());
            Assert.AreEqual(false, ca2.ShouldHaveValues());
            Assert.AreEqual(false, ca4.ShouldHaveValues());
            Assert.AreEqual(false, ca3.ShouldHaveValues());
            Assert.AreEqual(true, ca5.ShouldHaveValues());
        }

        [TestMethod()]
        public void CanBeUsedAsCondition_ReturnExpentedResult()
        {
            var ca = new ContactAttribute() { AttributeControlType = AttributeControlType.TextBox };
            var ca2 = new ContactAttribute() { AttributeControlType = AttributeControlType.MultilineTextbox };
            var ca3 = new ContactAttribute() { AttributeControlType = AttributeControlType.Datepicker };
            var ca4 = new ContactAttribute() { AttributeControlType = AttributeControlType.FileUpload };
            var ca5 = new ContactAttribute() { AttributeControlType = AttributeControlType.DropdownList };
            ContactAttribute ca6 = null;
            Assert.AreEqual(false, ca.CanBeUsedAsCondition());
            Assert.AreEqual(false, ca6.CanBeUsedAsCondition());
            Assert.AreEqual(false, ca2.CanBeUsedAsCondition());
            Assert.AreEqual(false, ca4.CanBeUsedAsCondition());
            Assert.AreEqual(false, ca3.CanBeUsedAsCondition());
            Assert.AreEqual(true, ca5.CanBeUsedAsCondition());
        }
    }
}
