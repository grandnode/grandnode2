using FluentValidation;
using FluentValidation.Results;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
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
    private readonly List<object> _records = [];
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
        _validator = new ShoppingCartValidator(new Mock<IMediator>().Object, _validatorFactoryMock.Object);
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
        await _validator.GetShoppingCartWarnings(cart, new List<CustomAttribute>(), context, true, true);

        //Assert
        var warnings = _records.OfType<ShoppingCartWarningsValidatorRecord>().Single();
        Assert.AreSame(context.Customer, warnings.Customer);
        Assert.AreSame(context.Store, warnings.Store);
        var attributes = _records.OfType<ShoppingCartCheckoutAttributesValidatorRecord>().Single();
        Assert.AreSame(context.Customer, attributes.Customer);
        Assert.AreSame(context.Store, attributes.Store);
        var amount = _records.OfType<ShoppingCartTotalAmountValidatorRecord>().Single();
        Assert.AreSame(context, amount.Context);
    }

    [TestMethod]
    public async Task GetRequiredProductWarnings_UsesTheGivenCustomerAndStore()
    {
        //Arrange
        var customer = new Customer();
        var product = new Product { RequireOtherProducts = true };

        //Act
        await _validator.GetRequiredProductWarnings(customer, new ShoppingCartItem(), product, "store-b");

        //Assert
        var record = _records.OfType<ShoppingCartRequiredProductValidatorRecord>().Single();
        Assert.AreSame(customer, record.Customer);
        Assert.AreEqual("store-b", record.StoreId);
    }
}
