using Grand.Business.Core.Commands.Messages.Tokens;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Messages.Commands.Handlers.Messages;
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
    private GetOrderTokensCommandHandler _handler;
    private Mock<IPriceFormatter> _priceFormatterMock;

    [TestInitialize]
    public void Init()
    {
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language { Id = "1" });
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(_orderCurrency);
        var giftVoucherService = new Mock<IGiftVoucherService>();
        giftVoucherService.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());
        _priceFormatterMock = new Mock<IPriceFormatter>();
        _priceFormatterMock.Setup(x => x.FormatTaxRate(It.IsAny<double>())).Returns("23");
        _priceFormatterMock.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");

        _handler = new GetOrderTokensCommandHandler(languageService.Object, currencyService.Object,
            new Mock<IProductService>().Object, new Mock<IVendorService>().Object, _priceFormatterMock.Object,
            new Mock<ICountryService>().Object, new Mock<IAddressAttributeParser>().Object,
            new Mock<IPaymentService>().Object, giftVoucherService.Object, new TaxSettings());
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
        Assert.AreEqual("EUR10", result.TaxRates["23"]);
    }
}
