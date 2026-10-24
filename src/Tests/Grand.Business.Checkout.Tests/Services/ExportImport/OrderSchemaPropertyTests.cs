using Grand.Business.Checkout.Services.ExportImport;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.ExportImport;

[TestClass]
public class OrderSchemaPropertyTests
{
    [TestMethod]
    public async Task CountryColumns_ReadCountryNames()
    {
        var countryService = new Mock<ICountryService>();
        countryService.Setup(x => x.GetAllCountries("", "", true))
            .ReturnsAsync(new List<Country> { new() { Id = "pl", Name = "Poland" } });
        var order = new Order {
            BillingAddress = new Address { CountryId = "pl" },
            ShippingAddress = new Address { CountryId = "unknown" }
        };
        var noAddresses = new Order();

        var properties = await new OrderSchemaProperty(countryService.Object).GetProperties([order, noAddresses]);
        var billing = properties.Single(x => x.PropertyName == "BillingCountry");
        var shipping = properties.Single(x => x.PropertyName == "ShippingCountry");

        Assert.AreEqual("Poland", billing.GetProperty(order));
        Assert.IsNull(shipping.GetProperty(order));
        Assert.AreEqual("", billing.GetProperty(noAddresses));
        Assert.AreEqual("", shipping.GetProperty(noAddresses));
        countryService.Verify(x => x.GetCountryById(It.IsAny<string>()), Times.Never);
    }
}
