using Grand.Business.Checkout.Services.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.CheckoutAttributes;

[TestClass]
public class CheckoutAttributeFormatterTests
{
    private readonly Customer _customer = new();
    private readonly Currency _eur = new() { CurrencyCode = "EUR" };
    private readonly DomainHost _host = new() { Url = "https://shop.example/" };
    private readonly Language _language = new() { Id = "pl" };
    private readonly Store _store = new() { Id = "store" };
    private CheckoutAttributeFormatter _formatter;
    private Mock<ICheckoutAttributeParser> _parserMock;
    private Mock<ITaxService> _taxServiceMock;

    [TestInitialize]
    public void Init()
    {
        _parserMock = new Mock<ICheckoutAttributeParser>();
        _taxServiceMock = new Mock<ITaxService>();
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.ConvertFromPrimaryStoreCurrency(12.3, _eur)).ReturnsAsync(11.69);
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(11.69, _eur)).Returns("€11.69");
        _formatter = new CheckoutAttributeFormatter(_parserMock.Object, currencyService.Object,
            _taxServiceMock.Object, priceFormatter.Object);
    }

    private CalculationContext Context(TaxDisplayType taxDisplayType) =>
        new(_customer, _store, _eur, taxDisplayType);

    [TestMethod]
    public async Task FormatAttributes_PricesTheValueInTheContextCurrencyAndTaxDisplay()
    {
        //Arrange
        var value = new CheckoutAttributeValue { Id = "yes", Name = "Yes", PriceAdjustment = 10 };
        var attribute = new CheckoutAttribute {
            Id = "wrap", Name = "Gift wrapping", AttributeControlTypeId = AttributeControlType.DropdownList,
            CheckoutAttributeValues = { value }
        };
        var selected = new List<CustomAttribute> { new() { Key = "wrap", Value = "yes" } };
        _parserMock.Setup(x => x.ParseCheckoutAttributes(selected)).ReturnsAsync(new List<CheckoutAttribute> { attribute });
        _taxServiceMock.Setup(x => x.GetCheckoutAttributePrice(attribute, value, true, _customer, _store))
            .ReturnsAsync((12.3, 23));

        //Act
        var result = await _formatter.FormatAttributes(selected, Context(TaxDisplayType.IncludingTax), _language, _host,
            htmlEncode: false);

        //Assert
        Assert.AreEqual("Gift wrapping: Yes [+€11.69]", result);
    }

    [TestMethod]
    public async Task FormatAttributes_TranslatesNamesIntoTheGivenLanguage()
    {
        //Arrange
        var attribute = new CheckoutAttribute {
            Id = "msg", Name = "Gift message", AttributeControlTypeId = AttributeControlType.TextBox,
            Locales = { new TranslationEntity { LanguageId = "pl", LocaleKey = "Name", LocaleValue = "Wiadomość" } }
        };
        var selected = new List<CustomAttribute> { new() { Key = "msg", Value = "Hi" } };
        _parserMock.Setup(x => x.ParseCheckoutAttributes(selected)).ReturnsAsync(new List<CheckoutAttribute> { attribute });

        //Act
        var result = await _formatter.FormatAttributes(selected, Context(TaxDisplayType.ExcludingTax), _language, _host,
            htmlEncode: false);

        //Assert
        Assert.AreEqual("Wiadomość: Hi", result);
    }

    [TestMethod]
    public async Task FormatAttributes_BuildsFileLinksOnTheGivenHost()
    {
        //Arrange
        var downloadGuid = Guid.NewGuid();
        var attribute = new CheckoutAttribute {
            Id = "file", Name = "Logo", TextPrompt = "Download", AttributeControlTypeId = AttributeControlType.FileUpload
        };
        var selected = new List<CustomAttribute> { new() { Key = "file", Value = downloadGuid.ToString() } };
        _parserMock.Setup(x => x.ParseCheckoutAttributes(selected)).ReturnsAsync(new List<CheckoutAttribute> { attribute });

        //Act
        var result = await _formatter.FormatAttributes(selected, Context(TaxDisplayType.ExcludingTax), _language, _host);

        //Assert
        StringAssert.Contains(result, $"https://shop.example/download/getfileupload/?downloadId={downloadGuid}");
    }
}
