using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Extensions
{
    [TestClass]
    public class ShippingExtensionsTests
    {
        [TestMethod]
        public void IsShippingRateMethodActive_ReturnExpectedResult()
        {
            var providerMock = new Mock<IShippingRateCalculationProvider>();
            providerMock.Setup(c => c.SystemName).Returns("dhl");
            var provider = providerMock.Object;
            var settings = new ShippingProviderSettings();
            Assert.IsFalse(provider.IsShippingRateMethodActive(settings));
            settings.ActiveSystemNames.Add("dhl");
            settings.ActiveSystemNames.Add("upc");
            Assert.IsTrue(provider.IsShippingRateMethodActive(settings));
        }

        [TestMethod]
        public void IsShippingRateMethodActive_NullProvider_ThrowException()
        {
            IShippingRateCalculationProvider provider = null;
            var settings = new ShippingProviderSettings();
            Assert.ThrowsException<ArgumentNullException>(() => provider.IsShippingRateMethodActive(settings));
        }

        [TestMethod]
        public void IsShippingRateMethodActive_NullSettings_ThrowException()
        {
            var providerMock = new Mock<IShippingRateCalculationProvider>();
            var provider = providerMock.Object;
            Assert.ThrowsException<ArgumentNullException>(() => provider.IsShippingRateMethodActive(null));
        }

        [TestMethod]
        public void CountryRestrictionExists_ReturnExpectedResult()
        {
            var method = new ShippingMethod();
            var countryId = "countryId";
            Assert.IsFalse(method.CountryRestrictionExists(countryId));
            method.RestrictedCountries.Add(new Domain.Directory.Country() { Id=countryId});
            Assert.IsTrue(method.CountryRestrictionExists(countryId));
        }

        [TestMethod]
        public void CustomerGroupRestrictionExists_ReturnExpectedResult()
        {
            var method = new ShippingMethod();
            var roleId= "role";
            Assert.IsFalse(method.CustomerGroupRestrictionExists(roleId));
            method.RestrictedGroups.Add(roleId);
            Assert.IsTrue(method.CustomerGroupRestrictionExists(roleId));
        }
    }
}
