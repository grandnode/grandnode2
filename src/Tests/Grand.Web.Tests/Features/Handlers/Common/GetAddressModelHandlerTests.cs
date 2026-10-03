using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Web.Features.Handlers.Common;
using Grand.Web.Features.Models.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Common;

[TestClass]
public class GetAddressModelHandlerTests
{
    private Mock<ICountryService> _countryServiceMock;
    private GetAddressModelHandler _handler;

    private readonly IList<Country> _countries = new List<Country> {
        new() { Id = "pl", Name = "Poland" },
        new() { Id = "de", Name = "Germany" }
    };

    [TestInitialize]
    public void Init()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _countryServiceMock.Setup(s => s.GetStateProvincesByCountryId(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<StateProvince>());
        var addressAttributeServiceMock = new Mock<IAddressAttributeService>();
        addressAttributeServiceMock.Setup(s => s.GetAllAddressAttributes(It.IsAny<string>()))
            .ReturnsAsync(new List<AddressAttribute>());

        _handler = new GetAddressModelHandler(_countryServiceMock.Object, new Mock<ITranslationService>().Object,
            addressAttributeServiceMock.Object, new Mock<IAddressAttributeParser>().Object,
            new Mock<IGroupService>().Object,
            new AddressSettings { CountryEnabled = true, StateProvinceEnabled = true });
    }

    private Task<Web.Models.Common.AddressModel> Handle(Address address, string defaultCountryId,
        Web.Models.Common.AddressModel model = null)
    {
        return _handler.Handle(new GetAddressModel {
            Model = model,
            Address = address,
            LoadCountries = () => _countries,
            Language = new Language { Id = "lang" },
            Store = new Domain.Stores.Store { Id = "store", DefaultCountryId = defaultCountryId }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task NewAddress_StartsInTheStoreDefaultCountry()
    {
        var model = await Handle(null, "de");

        Assert.AreEqual("de", model.CountryId);
        Assert.AreEqual("de", model.AvailableCountries.Single(x => x.Selected).Value);
        _countryServiceMock.Verify(s => s.GetStateProvincesByCountryId("de", "lang"));
    }

    [TestMethod]
    public async Task ExistingAddress_KeepsItsOwnCountry()
    {
        var model = await Handle(new Address { CountryId = "pl" }, "de");

        Assert.AreEqual("pl", model.CountryId);
    }

    [TestMethod]
    public async Task NewAddress_KeepsAPostedCountry()
    {
        var model = await Handle(null, "de", new Web.Models.Common.AddressModel { CountryId = "pl" });

        Assert.AreEqual("pl", model.CountryId);
    }

    [TestMethod]
    public async Task DefaultCountryNotOffered_LeavesTheCountryEmpty()
    {
        var model = await Handle(null, "us");

        Assert.IsNull(model.CountryId);
        Assert.IsFalse(model.AvailableCountries.Any(x => x.Selected));
    }
}
