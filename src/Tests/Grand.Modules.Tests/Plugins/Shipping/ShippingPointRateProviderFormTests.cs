using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.TypeConverters;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shipping.ShippingPoint;
using Shipping.ShippingPoint.Domain;
using Shipping.ShippingPoint.Services;

namespace Grand.Modules.Tests.Plugins.Shipping;

[TestClass]
public class ShippingPointRateProviderFormTests
{
    private const string PluginName = "Shipping point";
    private const string SelectFirst = "Select a point";
    private readonly Customer _customer = new();
    private readonly Mock<ICustomerService> _customers = new();
    private readonly Mock<IShippingPointService> _points = new();
    private readonly ShippingPointRateProvider _provider;

    public ShippingPointRateProviderFormTests()
    {
        new WebTypeConverter().Register();
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource("Shipping.ShippingPoint.PluginName")).Returns(PluginName);
        translation.Setup(x => x.GetResource("Shipping.ShippingPoint.SelectBeforeProceed")).Returns(SelectFirst);
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext.CurrentCustomer).Returns(_customer);
        context.Setup(x => x.WorkContext.WorkingCurrency).Returns(new Currency());
        context.Setup(x => x.StoreContext.CurrentStore).Returns(new Store { Id = "store1" });
        var currency = new Mock<ICurrencyService>();
        currency.Setup(x => x.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), It.IsAny<Currency>()))
            .ReturnsAsync((double fee, Currency _) => fee);
        _provider = new ShippingPointRateProvider(_points.Object, translation.Object, context.Object, _customers.Object,
            new Mock<ICountryService>().Object, currency.Object, new ShippingPointRateSettings());
    }

    private void Offered(params string[] names)
    {
        var options = names.Select(x => new ShippingOption { Name = x }).ToList();
        _customer.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.OfferedShippingOptions, StoreId = "store1",
            Value = CommonHelper.To<string>(options)
        });
    }

    private Task<IList<string>> Post(ShippingPoints point)
    {
        _points.Setup(x => x.GetStoreShippingPointById(point.Id)).ReturnsAsync(point);
        return _provider.ValidateShippingForm(PluginName + ":", new Dictionary<string, string> { ["selectedShippingOption"] = point.Id });
    }

    [TestMethod]
    public async Task PointOfAnotherStore_AskedToSelect()
    {
        Offered(PluginName);

        var errors = await Post(new ShippingPoints { Id = "p2", StoreId = "store2", PickupFee = 0 });

        CollectionAssert.AreEqual(new[] { SelectFirst }, errors.ToArray());
        _customers.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OfferedOptionsWithoutThePoint_AskedToSelect()
    {
        Offered("Ground");

        var errors = await Post(new ShippingPoints { Id = "p1", StoreId = "store1", PickupFee = 3 });

        CollectionAssert.AreEqual(new[] { SelectFirst }, errors.ToArray());
    }

    [TestMethod]
    public async Task PointOfTheStore_ItsFeeIsTheRate()
    {
        Offered(PluginName);

        var errors = await Post(new ShippingPoints { Id = "p1", StoreId = "store1", PickupFee = 3 });

        Assert.AreEqual(0, errors.Count);
        _customers.Verify(x => x.UpdateUserField(_customer, SystemCustomerFieldNames.OfferedShippingOptions,
            It.Is<List<ShippingOption>>(o => o.Single().Rate == 3), "store1"), Times.Once);
    }

    [TestMethod]
    public async Task PointOfEveryStore_Accepted()
    {
        Offered(PluginName);

        var errors = await Post(new ShippingPoints { Id = "p0", StoreId = "", PickupFee = 1 });

        Assert.AreEqual(0, errors.Count);
    }
}
