using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class LoyaltyPointsCalculationServiceTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private LoyaltyPointsSettings _loyaltyPointsSettings;
    private LoyaltyPointsCalculationService _service;
    private ShoppingCartSettings _shoppingCartSettings;

    [TestInitialize]
    public void Init()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _loyaltyPointsSettings = new LoyaltyPointsSettings();
        _shoppingCartSettings = new ShoppingCartSettings();

        _service = new LoyaltyPointsCalculationService(_loyaltyPointsServiceMock.Object,
            _currencyServiceMock.Object, _loyaltyPointsSettings, _shoppingCartSettings);
    }

    [TestMethod]
    public async Task ConvertLoyaltyPointsToAmountTest()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 1;
        //Act
        var result = await _service.ConvertLoyaltyPointsToAmount(10);
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public async Task ConvertLoyaltyPointsToAmount_RoundsInThePrimaryStoreCurrency()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 0.33;
        _shoppingCartSettings.RoundPrices = true;
        _currencyServiceMock.Setup(x => x.GetPrimaryStoreCurrency())
            .ReturnsAsync(new Currency { CurrencyCode = "USD", NumberDecimal = 2 });
        //Act
        var result = await _service.ConvertLoyaltyPointsToAmount(15);
        //Assert
        Assert.AreEqual(4.95, result, 0.0001);
    }

    [TestMethod]
    public void ConvertAmountToLoyaltyPointsTest()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 1;
        //Act
        var result = _service.ConvertAmountToLoyaltyPoints(10);
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void CheckMinimumLoyaltyPointsToUseRequirementTest()
    {
        //Assert
        _loyaltyPointsSettings.MinimumLoyaltyPointsToUse = 20;
        //Act
        var result = _service.CheckMinimumLoyaltyPointsToUseRequirement(10);
        //Assert
        Assert.IsFalse(result);
    }
}
