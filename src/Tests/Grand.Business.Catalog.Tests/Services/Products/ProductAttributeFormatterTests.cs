using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductAttributeFormatterTests
{
    private Mock<IPriceFormatter> _priceFormatterMock;
    private Mock<IPricingService> _pricingServiceMock;
    private ProductAttributeFormatter _productAttributeFormatter;
    private Mock<IProductAttributeService> _productAttributeServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<ITaxService> _taxServiceMock;
    private CalculationContext _context;
    private Language _language;
    private DomainHost _host;

    [TestInitialize]
    public void InitializeTests()
    {
        _productAttributeServiceMock = new Mock<IProductAttributeService>();
        _productAttributeServiceMock.Setup(x => x.GetProductAttributeById(It.IsAny<string>()))
            .Returns(Task.FromResult(new ProductAttribute {
                Name = "test",
                Locales = { new TranslationEntity { LanguageId = "pl", LocaleKey = "Name", LocaleValue = "test-pl" } }
            }));
        _taxServiceMock = new Mock<ITaxService>();
        _priceFormatterMock = new Mock<IPriceFormatter>();
        _pricingServiceMock = new Mock<IPricingService>();
        _productServiceMock = new Mock<IProductService>();
        _productAttributeFormatter = new ProductAttributeFormatter(_productAttributeServiceMock.Object,
            _taxServiceMock.Object, _priceFormatterMock.Object, _pricingServiceMock.Object, _productServiceMock.Object);

        _context = new CalculationContext(new Customer { Id = "c1" }, new Store { Id = "s1" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);
        _language = new Language { Id = "en" };
        _host = new DomainHost { Url = "https://shop.example/" };
    }

    private static Product ProductWithCheckboxValue(double priceAdjustment = 0)
    {
        var product = new Product();
        var productAttributeMapping = new ProductAttributeMapping
            { Id = "1", AttributeControlTypeId = AttributeControlType.Checkboxes };
        productAttributeMapping.ProductAttributeValues.Add(new ProductAttributeValue
            { Id = "1", Name = "aa", PriceAdjustment = priceAdjustment });
        product.ProductAttributeMappings.Add(productAttributeMapping);
        return product;
    }

    [TestMethod]
    public async Task FormatAttributesTest_EmptyCustomAttribute()
    {
        //Arrange
        var product = new Product();
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute>(),
            _context, _language, _host);
        //Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public async Task FormatAttributesTest_GiftVoucher()
    {
        //Arrange
        var product = new Product { GiftVoucherTypeId = GiftVoucherType.Virtual, IsGiftVoucher = true };
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "RecipientName", Value = "John" },
            new() { Key = "RecipientEmail", Value = "John@john.com" },
            new() { Key = "SenderName", Value = "Will" },
            new() { Key = "SenderEmail", Value = "Will@will.com" },
            new() { Key = "Message", Value = "" }
        }, _context, _language, _host);
        //Assert
        Assert.AreEqual("Will &lt;Will@will.com&gt;<br />John &lt;John@john.com&gt;", result);
    }

    [TestMethod]
    public async Task FormatAttributesTest_CustomAttribute()
    {
        //Arrange
        var product = ProductWithCheckboxValue();
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "1", Value = "1" }
        }, _context, _language, _host);
        //Assert
        Assert.AreEqual("test: aa", result);
    }

    [TestMethod]
    public async Task FormatAttributes_TranslatesNamesIntoTheGivenLanguage()
    {
        //Arrange
        var product = ProductWithCheckboxValue();
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "1", Value = "1" }
        }, _context, new Language { Id = "pl" }, _host);
        //Assert
        Assert.AreEqual("test-pl: aa", result);
    }

    [TestMethod]
    public async Task FormatAttributes_PricesTheAdjustmentForTheGivenContext()
    {
        //Arrange
        var product = ProductWithCheckboxValue(5);
        _pricingServiceMock.Setup(x => x.GetProductAttributeValuePriceAdjustment(It.IsAny<ProductAttributeValue>(),
                _context.Customer, _context.Store, _context.Currency))
            .ReturnsAsync(5);
        _taxServiceMock.Setup(x => x.GetProductPrice(product, 5, true, _context.Customer, _context.Store))
            .ReturnsAsync((6, 20));
        _priceFormatterMock.Setup(x => x.FormatPrice(6, _context.Currency)).Returns("6 EUR");
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "1", Value = "1" }
        }, _context, _language, _host);
        //Assert
        Assert.AreEqual("test: aa [+6 EUR]", result);
    }

    [TestMethod]
    public async Task FormatAttributes_WithoutPrices_DoesNotNeedAContext()
    {
        //Arrange
        var product = ProductWithCheckboxValue(5);
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "1", Value = "1" }
        }, null, _language, _host, renderPrices: false);
        //Assert
        Assert.AreEqual("test: aa", result);
        _pricingServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task FormatAttributes_FileUpload_LinksToTheGivenHost()
    {
        //Arrange
        var product = new Product();
        product.ProductAttributeMappings.Add(new ProductAttributeMapping
            { Id = "1", AttributeControlTypeId = AttributeControlType.FileUpload, TextPrompt = "file" });
        var downloadGuid = Guid.NewGuid();
        //Act
        var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute> {
            new() { Key = "1", Value = downloadGuid.ToString() }
        }, _context, _language, _host);
        //Assert
        Assert.AreEqual(
            $"test: <a href=\"https://shop.example/download/getfileupload/?downloadId={downloadGuid}\" class=\"fileuploadattribute\">file</a>",
            result);
    }
}
