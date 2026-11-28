using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetOrderTokensCommandHandlerTests
{
    private readonly Currency _orderCurrency = new() { CurrencyCode = "EUR" };
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;
    private GetOrderTokensCommandHandler _handler;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPriceFormatter> _priceFormatterMock;
    private Mock<ISettingService> _settingServiceMock;

    public GetOrderTokensCommandHandlerTests()
    {
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language { Id = "1" });
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(_orderCurrency);
        _giftVoucherServiceMock = new Mock<IGiftVoucherService>();
        _giftVoucherServiceMock.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());
        _priceFormatterMock = new Mock<IPriceFormatter>();
        _priceFormatterMock.Setup(x => x.FormatTaxRate(It.IsAny<double>())).Returns("23");
        _priceFormatterMock.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");

        _paymentServiceMock = new Mock<IPaymentService>();
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(x => x.LoadSetting<TaxSettings>(It.IsAny<string>())).ReturnsAsync(new TaxSettings());

        _handler = new GetOrderTokensCommandHandler(languageService.Object, currencyService.Object,
            new Mock<IProductService>().Object, new Mock<IVendorService>().Object, _priceFormatterMock.Object,
            new Mock<ICountryService>().Object, new Mock<IAddressAttributeParser>().Object,
            _paymentServiceMock.Object, _giftVoucherServiceMock.Object, _settingServiceMock.Object);
    }

    [TestMethod]
    public async Task Handle_FormatsTaxRatesInTheOrderCurrency()
    {
        //Arrange
        var order = new Order { CustomerCurrencyCode = "EUR", CustomerLanguageId = "1", OrderTax = 10 };
        order.OrderTaxes.Add(new OrderTax { Percent = 23, Amount = 10 });

        //Act
        var result = await _handler.Handle(new GetOrderTokensCommand {
            Order = order, Customer = new Customer(), Store = new Store(), Host = new DomainHost { Url = "https://localhost/" }
        }, CancellationToken.None);

        //Assert
        Assert.IsNotNull(result.TaxRates);
        Assert.AreEqual("EUR10", result.TaxRates["23"]);
    }

    [TestMethod]
    public async Task Handle_PaymentMethodNameInTheCustomerLanguage()
    {
        //Arrange
        var paymentProvider = new Mock<IPaymentProvider>();
        paymentProvider.Setup(x => x.FriendlyName).Returns("Request language");
        paymentProvider.Setup(x => x.GetFriendlyName("1")).Returns("Customer language");
        _paymentServiceMock.Setup(x => x.LoadPaymentMethodBySystemName("Payments.Test"))
            .Returns(paymentProvider.Object);
        var order = new Order {
            CustomerCurrencyCode = "EUR", CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test"
        };

        //Act
        var result = await _handler.Handle(new GetOrderTokensCommand {
            Order = order, Customer = new Customer(), Store = new Store(), Host = new DomainHost { Url = "https://localhost/" }
        }, CancellationToken.None);

        //Assert
        Assert.AreEqual("Customer language", result.PaymentMethod);
    }

    [TestMethod]
    public async Task Handle_TaxSettingsOfTheMessagesStore()
    {
        //Arrange - sent from a scheduled task or another store's admin, the current store is not the order's.
        //An order without a store gets the store the message falls back to, the one the store tokens read
        _settingServiceMock.Setup(x => x.LoadSetting<TaxSettings>("store2"))
            .ReturnsAsync(new TaxSettings { DisplayTaxRates = true });
        var order = new Order { StoreId = "", CustomerCurrencyCode = "EUR", CustomerLanguageId = "1", OrderTax = 10 };
        order.OrderTaxes.Add(new OrderTax { Percent = 23, Amount = 10 });

        //Act
        var result = await _handler.Handle(new GetOrderTokensCommand {
            Order = order, Customer = new Customer(), Store = new Store { Id = "store2" },
            Host = new DomainHost { Url = "https://localhost/" }
        }, CancellationToken.None);

        //Assert
        Assert.IsTrue(result.DisplayTaxRates);
        _settingServiceMock.Verify(x => x.LoadSetting<TaxSettings>("store2"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_GiftVoucherDeletedAfterUse_StillShowsTheAmount()
    {
        //Arrange
        _giftVoucherServiceMock.Setup(x => x.GetAllGiftVoucherUsageHistory("order1"))
            .ReturnsAsync([new GiftVoucherUsageHistory { GiftVoucherId = "deleted", UsedValue = 5 }]);
        var order = new Order { Id = "order1", CustomerCurrencyCode = "EUR", CustomerLanguageId = "1" };

        //Act
        var result = await _handler.Handle(new GetOrderTokensCommand {
            Order = order, Customer = new Customer(), Store = new Store(), Host = new DomainHost { Url = "https://localhost/" }
        }, CancellationToken.None);

        //Assert
        Assert.IsNotNull(result.GiftVouchers);
        Assert.AreEqual("EUR-5", result.GiftVouchers[""]);
    }

    [TestMethod]
    public async Task Handle_GiftVoucherUsedTwice_OneLineWithTheSum()
    {
        //Arrange
        _giftVoucherServiceMock.Setup(x => x.GetAllGiftVoucherUsageHistory("order1")).ReturnsAsync([
            new GiftVoucherUsageHistory { GiftVoucherId = "gv1", UsedValue = 5 },
            new GiftVoucherUsageHistory { GiftVoucherId = "gv1", UsedValue = 3 }
        ]);
        _giftVoucherServiceMock.Setup(x => x.GetGiftVoucherById("gv1"))
            .ReturnsAsync(new GiftVoucher { Id = "gv1", Code = "CODE-1" });
        var order = new Order { Id = "order1", CustomerCurrencyCode = "EUR", CustomerLanguageId = "1" };

        //Act
        var result = await _handler.Handle(new GetOrderTokensCommand {
            Order = order, Customer = new Customer(), Store = new Store(), Host = new DomainHost { Url = "https://localhost/" }
        }, CancellationToken.None);

        //Assert
        Assert.IsNotNull(result.GiftVouchers);
        Assert.HasCount(1, result.GiftVouchers);
        Assert.AreEqual("EUR-8", result.GiftVouchers["CODE-1"]);
    }
}
