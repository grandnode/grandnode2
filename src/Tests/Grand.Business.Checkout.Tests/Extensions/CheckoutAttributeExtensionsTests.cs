using Grand.Business.Checkout.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Extensions
{
    [TestClass]
    public class CheckoutAttributeExtensionsTests
    {
        [TestMethod()]
        public void CanRemoveShippableAttributes()
        {
            List<CheckoutAttribute> attributes = new List<CheckoutAttribute>();
            attributes.Add(new CheckoutAttribute { Id = "1", Name = "Attribute001", ShippableProductRequired = false });
            attributes.Add(new CheckoutAttribute { Id = "2", Name = "Attribute002", ShippableProductRequired = true });
            attributes.Add(new CheckoutAttribute { Id = "3", Name = "Attribute003", ShippableProductRequired = false });
            attributes.Add(new CheckoutAttribute { Id = "4", Name = "Attribute004", ShippableProductRequired = true });

            //removes these with "ShippableProductRequired = true"
            IList<CheckoutAttribute> afterRemoval = attributes.RemoveShippableAttributes();
            Assert.AreEqual(2, afterRemoval.Count());
            Assert.AreEqual("Attribute001", afterRemoval[0].Name);
            Assert.AreEqual("3", afterRemoval[1].Id);
        }

        [TestMethod]
        public void ShouldHaveValues_ReturnExpectedResults()
        {
            List<CheckoutAttribute> attributes = new List<CheckoutAttribute>();
            var atr1=new CheckoutAttribute { Id = "1", Name = "Attribute001", AttributeControlTypeId=AttributeControlType.Checkboxes };
            var atr2 = new CheckoutAttribute { Id = "2", Name = "Attribute002", AttributeControlTypeId = AttributeControlType.TextBox};
            var atr3 = new CheckoutAttribute { Id = "3", Name = "Attribute003", AttributeControlTypeId = AttributeControlType.Datepicker};
            var atr4 = new CheckoutAttribute { Id = "4", Name = "Attribute004", AttributeControlTypeId = AttributeControlType.FileUpload };
            var atr5 = new CheckoutAttribute { Id = "5", Name = "Attribute005", AttributeControlTypeId = AttributeControlType.DropdownList };
            CheckoutAttribute atr6 = null;
            Assert.IsTrue(atr1.ShouldHaveValues());
            Assert.IsTrue(atr5.ShouldHaveValues());
            Assert.IsFalse(atr2.ShouldHaveValues());
            Assert.IsFalse(atr3.ShouldHaveValues());
            Assert.IsFalse(atr4.ShouldHaveValues());
            Assert.IsFalse(atr6.ShouldHaveValues());
        }
    }
}
