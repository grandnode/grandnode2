using FluentValidation;
using FluentValidation.Results;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Events.Checkout.ShoppingCart;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IValidatorFactory = Grand.Infrastructure.Validators.IValidatorFactory;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class ShoppingCartValidatorTests
{
    private static readonly Language Language = new() { Id = "lang1" };
    private readonly List<object> _records = [];
    private Mock<IMediator> _mediatorMock;
    private ShoppingCartValidator _validator;
    private Mock<IValidatorFactory> _validatorFactoryMock;

    [TestInitialize]
    public void Init()
    {
        _validatorFactoryMock = new Mock<IValidatorFactory>();
        Capture<ShoppingCartWarningsValidatorRecord>();
        Capture<ShoppingCartCheckoutAttributesValidatorRecord>();
        Capture<ShoppingCartTotalAmountValidatorRecord>();
        Capture<ShoppingCartRequiredProductValidatorRecord>();
        _mediatorMock = new Mock<IMediator>();
        _validator = new ShoppingCartValidator(_mediatorMock.Object, _validatorFactoryMock.Object);
    }

    private void Capture<T>()
    {
        var validator = new Mock<IValidator<T>>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback((T record, CancellationToken _) => _records.Add(record))
            .ReturnsAsync(new ValidationResult());
        _validatorFactoryMock.Setup(x => x.GetValidator<T>()).Returns(validator.Object);
    }

    [TestMethod]
    public async Task GetShoppingCartWarnings_ValidatesForTheContextCustomerAndStore()
    {
        //Arrange
        var context = new CalculationContext(new Customer(), new Store { Id = "store" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);
        var cart = new List<ShoppingCartItem>();

        //Act
        await _validator.GetShoppingCartWarnings(cart, new List<CustomAttribute>(), context, Language, true, true);

        //Assert
        var warnings = _records.OfType<ShoppingCartWarningsValidatorRecord>().Single();
        Assert.AreSame(context.Customer, warnings.Customer);
        Assert.AreSame(context.Store, warnings.Store);
        Assert.AreSame(Language, warnings.Language);
        var attributes = _records.OfType<ShoppingCartCheckoutAttributesValidatorRecord>().Single();
        Assert.AreSame(context.Customer, attributes.Customer);
        Assert.AreSame(context.Store, attributes.Store);
        Assert.AreSame(Language, attributes.Language);
        var amount = _records.OfType<ShoppingCartTotalAmountValidatorRecord>().Single();
        Assert.AreSame(context, amount.Context);
        Assert.AreSame(Language, amount.Language);
    }

    [TestMethod]
    public async Task GetShoppingCartWarnings_ValidateAmountFalse_DoesNotRunTotalAmount()
    {
        //Arrange
        var context = new CalculationContext(new Customer(), new Store { Id = "store" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);

        //Act
        await _validator.GetShoppingCartWarnings(new List<ShoppingCartItem>(), new List<CustomAttribute>(), context,
            Language, true, false);

        //Assert
        Assert.AreEqual(0, _records.OfType<ShoppingCartTotalAmountValidatorRecord>().Count());
    }

    [TestMethod]
    public async Task GetRequiredProductWarnings_UsesTheGivenCustomerAndStore()
    {
        //Arrange
        var customer = new Customer();
        var product = new Product { RequireOtherProducts = true };

        //Act
        await _validator.GetRequiredProductWarnings(customer, new ShoppingCartItem(), product, "store-b", Language);

        //Assert
        var record = _records.OfType<ShoppingCartRequiredProductValidatorRecord>().Single();
        Assert.AreSame(customer, record.Customer);
        Assert.AreEqual("store-b", record.StoreId);
        Assert.AreSame(Language, record.Language);
    }

    [TestMethod]
    public async Task GetShoppingCartWarnings_PublishesTheWarningsWithTheLanguage()
    {
        //Arrange
        var context = new CalculationContext(new Customer(), new Store { Id = "store" },
            new Currency { CurrencyCode = "EUR" }, TaxDisplayType.IncludingTax);

        //Act
        await _validator.GetShoppingCartWarnings(new List<ShoppingCartItem>(), null, context, Language, false, false);

        //Assert
        _mediatorMock.Verify(x => x.Publish(
            It.Is<ShoppingCartWarningsEvent<string, ShoppingCartItem>>(e => e.Language == Language),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetShoppingCartItemWarnings_PublishesTheWarningsWithTheLanguage()
    {
        //Act
        await _validator.GetShoppingCartItemWarnings(new Customer(), new ShoppingCartItem(), new Product(),
            NoChecks(), Language);

        //Assert
        _mediatorMock.Verify(x => x.Publish(
            It.Is<ShoppingCartItemWarningsEvent<Customer, ShoppingCartItem, Product>>(e => e.Language == Language),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetShoppingCartItemWarnings_WithoutLanguage_Throws()
    {
        //Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validator.GetShoppingCartItemWarnings(new Customer(), new ShoppingCartItem(), new Product(), NoChecks(),
                null));
    }

    private static ShoppingCartValidatorOptions NoChecks() => new() {
        GetStandardWarnings = false, GetInventoryWarnings = false, GetAttributesWarnings = false,
        GetGiftVoucherWarnings = false, GetRequiredProductWarnings = false, GetReservationWarnings = false
    };
}