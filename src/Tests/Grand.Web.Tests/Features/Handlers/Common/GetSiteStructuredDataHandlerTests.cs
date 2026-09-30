using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Features.Handlers.Common;
using Grand.Web.Features.Models.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Common;

[TestClass]
public class GetSiteStructuredDataHandlerTests
{
    private readonly Domain.Stores.Store _store = new() { Id = "store-1", Name = "Shop" };
    private Mock<ICountryService> _countryServiceMock;
    private OrderSettings _orderSettings;
    private StoreInformationSettings _storeInformationSettings;
    private GetSiteStructuredDataHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _countryServiceMock.Setup(x => x.GetAllCountriesForShipping(It.IsAny<string>(), "store-1", false))
            .ReturnsAsync(new List<Country> { new() { TwoLetterIsoCode = "PL" } });
        _orderSettings = new OrderSettings();
        _storeInformationSettings = new StoreInformationSettings { FacebookLink = "https://facebook.com/shop" };
        _handler = new GetSiteStructuredDataHandler(_countryServiceMock.Object, new Mock<IPictureService>().Object,
            _orderSettings, _storeInformationSettings);
    }

    [TestMethod]
    public async Task Handle_ReturnsDisabled_NoReturnPolicy()
    {
        var result = await _handler.Handle(new GetSiteStructuredData { Store = _store }, default);

        Assert.IsNull(result.ReturnPolicy);
        Assert.AreEqual("Shop", result.StoreName);
        CollectionAssert.AreEqual(new[] { "https://facebook.com/shop" }, result.SameAs.ToArray());
    }

    [TestMethod]
    public async Task Handle_ReturnsEnabledWithDayLimit_FiniteWindow()
    {
        _orderSettings.MerchandiseReturnsEnabled = true;
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 14;

        var result = await _handler.Handle(new GetSiteStructuredData { Store = _store }, default);

        Assert.IsNotNull(result.ReturnPolicy);
        Assert.IsFalse(result.ReturnPolicy.UnlimitedWindow);
        CollectionAssert.AreEqual(new[] { "PL" }, result.ReturnPolicy.CountryCodes.ToArray());
    }

    [TestMethod]
    public async Task Handle_ReturnsEnabledWithoutDayLimit_UnlimitedWindow()
    {
        _orderSettings.MerchandiseReturnsEnabled = true;
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 0;

        var result = await _handler.Handle(new GetSiteStructuredData { Store = _store }, default);

        Assert.IsTrue(result.ReturnPolicy.UnlimitedWindow);
    }

    [TestMethod]
    public async Task Handle_ReturnFeesAndMethodDeclared_MappedToSchemaOrg()
    {
        _orderSettings.MerchandiseReturnsEnabled = true;
        _orderSettings.MerchandiseReturnFees = MerchandiseReturnFees.Free;
        _orderSettings.MerchandiseReturnMethod = MerchandiseReturnMethod.ByMail;

        var result = await _handler.Handle(new GetSiteStructuredData { Store = _store }, default);

        Assert.AreEqual("https://schema.org/FreeReturn", result.ReturnPolicy.ReturnFees);
        Assert.AreEqual("https://schema.org/ReturnByMail", result.ReturnPolicy.ReturnMethod);
    }

    [TestMethod]
    public async Task Handle_ReturnFeesAndMethodNotSpecified_Null()
    {
        _orderSettings.MerchandiseReturnsEnabled = true;

        var result = await _handler.Handle(new GetSiteStructuredData { Store = _store }, default);

        Assert.IsNull(result.ReturnPolicy.ReturnFees);
        Assert.IsNull(result.ReturnPolicy.ReturnMethod);
    }
}
