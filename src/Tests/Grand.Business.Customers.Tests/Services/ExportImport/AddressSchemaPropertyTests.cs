using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Customers.Services.ExportImport;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services.ExportImport;

[TestClass]
public class AddressSchemaPropertyTests
{
    [TestMethod]
    public async Task CountryColumn_ReadsCountryName()
    {
        var countryService = new Mock<ICountryService>();
        countryService.Setup(x => x.GetAllCountries("", "", true))
            .ReturnsAsync(new List<Country> { new() { Id = "pl", Name = "Poland" } });
        var address = new Address { CountryId = "pl" };
        var noCountry = new Address();

        var properties = await new AddressSchemaProperty(countryService.Object).GetProperties([address, noCountry]);
        var country = properties.Single(x => x.PropertyName == "Country");

        Assert.AreEqual("Poland", country.GetProperty(address));
        Assert.AreEqual("", country.GetProperty(noCountry));
        countryService.Verify(x => x.GetCountryById(It.IsAny<string>()), Times.Never);
    }
}
