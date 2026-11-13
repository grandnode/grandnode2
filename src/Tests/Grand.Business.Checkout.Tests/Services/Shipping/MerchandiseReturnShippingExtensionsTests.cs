using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Shipping;

[TestClass]
public class MerchandiseReturnShippingExtensionsTests
{
    [TestMethod]
    public void CustomerCost_FeesFree_IsZero()
    {
        Assert.AreEqual(0, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.Free, false));
    }

    [TestMethod]
    public void CustomerCost_AnyFreeReason_IsZero()
    {
        Assert.AreEqual(0, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.CustomerPays, true));
    }

    [TestMethod]
    public void CustomerCost_NotSpecified_KeepsOptionCost()
    {
        Assert.AreEqual(12.5, MerchandiseReturnShippingExtensions.CustomerCost(12.5, MerchandiseReturnFees.NotSpecified, false));
    }

    [TestMethod]
    public void LabelLockKey_DiffersFromQuantityLockKey()
    {
        Assert.AreNotEqual(MerchandiseReturnExtensions.QuantityLockKey("x"), MerchandiseReturnExtensions.LabelLockKey("x"));
    }

    [TestMethod]
    public async Task AddressLine_LeadingStreetZipCityCountry()
    {
        var countryService = new Mock<ICountryService>();
        countryService.Setup(x => x.GetCountryById("pl")).ReturnsAsync(new Country { Name = "Poland" });
        var address = new Address
            { Address1 = "Main 1", ZipPostalCode = "00-001", City = "Warsaw", CountryId = "pl" };

        var line = await MerchandiseReturnShippingExtensions.AddressLine(countryService.Object, address, "Shop");

        Assert.AreEqual("Shop, Main 1, 00-001 Warsaw, Poland", line);
    }

    [TestMethod]
    public async Task AddressLine_EmptyPartsAndNoAddress_LeftOut()
    {
        var countryService = new Mock<ICountryService>();

        Assert.AreEqual("Warsaw", await MerchandiseReturnShippingExtensions.AddressLine(countryService.Object,
            new Address { City = "Warsaw" }, null, ""));
        Assert.AreEqual("Point", await MerchandiseReturnShippingExtensions.AddressLine(countryService.Object,
            (Address)null, "Point"));
        countryService.Verify(x => x.GetCountryById(It.IsAny<string>()), Times.Never);
    }
}
