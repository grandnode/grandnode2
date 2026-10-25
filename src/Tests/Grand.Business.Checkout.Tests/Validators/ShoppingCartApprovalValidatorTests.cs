using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Validators;

[TestClass]
public class ShoppingCartApprovalValidatorTests
{
    private static readonly Language Language = new() { Id = "lang1" };
    private Mock<ICurrencyService> _currencyServiceMock;
    private CalculationContext _context;
    private Mock<IOrderApprovalService> _orderApprovalServiceMock;
    private Mock<IPriceFormatter> _priceFormatterMock;
    private Mock<ITranslationService> _translationServiceMock;
    private ShoppingCartApprovalValidator _validator;

    [TestInitialize]
    public void Init()
    {
        _orderApprovalServiceMock = new Mock<IOrderApprovalService>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _priceFormatterMock = new Mock<IPriceFormatter>();
        _translationServiceMock = new Mock<ITranslationService>();
        _context = new CalculationContext(new Customer(), new Store { Id = "store" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);
        _validator = new ShoppingCartApprovalValidator(_orderApprovalServiceMock.Object, _currencyServiceMock.Object,
            _priceFormatterMock.Object, _translationServiceMock.Object);
    }

    [TestMethod]
    public async Task Validate_Blocked_FailsWithFormattedLimit()
    {
        //Arrange
        var cart = new List<ShoppingCartItem> { new() };
        _orderApprovalServiceMock.Setup(x => x.GetCheckoutState(cart, _context))
            .ReturnsAsync(new OrderApprovalCheckoutState(true, 100, 150, null));
        _currencyServiceMock.Setup(x => x.ConvertFromPrimaryStoreCurrency(100, _context.Currency)).ReturnsAsync(110);
        _priceFormatterMock.Setup(x => x.FormatPrice(110, _context.Currency)).Returns("110.00 EUR");
        _translationServiceMock.Setup(x => x.GetResource("Checkout.OrderApproval.Required", Language.Id, It.IsAny<string>(), It.IsAny<bool>()))
            .Returns("An approval is required above {0}.");

        //Act
        var result = await _validator.ValidateAsync(new ShoppingCartApprovalValidatorRecord(cart, _context, Language));

        //Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("An approval is required above 110.00 EUR.", result.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    public async Task Validate_NotBlocked_Passes()
    {
        //Arrange
        var cart = new List<ShoppingCartItem> { new() };
        _orderApprovalServiceMock.Setup(x => x.GetCheckoutState(cart, _context))
            .ReturnsAsync(new OrderApprovalCheckoutState(false, null, 50, null));

        //Act
        var result = await _validator.ValidateAsync(new ShoppingCartApprovalValidatorRecord(cart, _context, Language));

        //Assert
        Assert.IsTrue(result.IsValid);
        _currencyServiceMock.Verify(
            x => x.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), It.IsAny<Currency>()), Times.Never);
    }

    [TestMethod]
    public async Task Validate_RequiredAndApproved_Passes()
    {
        //Arrange
        var cart = new List<ShoppingCartItem> { new() };
        _orderApprovalServiceMock.Setup(x => x.GetCheckoutState(cart, _context))
            .ReturnsAsync(new OrderApprovalCheckoutState(true, 100, 150,
                new OrderApproval { Status = OrderApprovalStatus.Approved }));

        //Act
        var result = await _validator.ValidateAsync(new ShoppingCartApprovalValidatorRecord(cart, _context, Language));

        //Assert
        Assert.IsTrue(result.IsValid);
    }
}
