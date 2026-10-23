using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class OrderProductViewModelServiceTests
{
    private Mock<IMediator> _mediator;
    private OrderProductViewModelService _service;
    private readonly Order _order = new() { Id = "o1", CustomerId = "c1", StoreId = "s1", CustomerCurrencyCode = "USD" };
    private readonly Product _product = new() { Id = "p1" };

    [TestInitialize]
    public void Setup()
    {
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(new Customer());
        var storeService = new Mock<IStoreService>();
        storeService.Setup(s => s.GetStoreById("s1")).ReturnsAsync(new Store());
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(s => s.GetCurrencyByCode("USD")).ReturnsAsync(new Currency());
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(s => s.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language());
        var validator = new Mock<IShoppingCartValidator>();
        validator.Setup(v => v.GetShoppingCartItemAttributeWarnings(It.IsAny<Customer>(), _product,
            It.IsAny<ShoppingCartItem>(), It.IsAny<Language>(), It.IsAny<bool>())).ReturnsAsync(new List<string>());
        validator.Setup(v => v.GetShoppingCartItemGiftVoucherWarnings(It.IsAny<Customer>(), _product,
            It.IsAny<ShoppingCartItem>(), It.IsAny<Language>())).ReturnsAsync(new List<string>());
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(new Mock<IWorkContext>().Object);
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(name => name);
        _mediator = new Mock<IMediator>();
        _service = new OrderProductViewModelService(
            new Mock<IProductAttributeService>().Object, new Mock<IProductAttributeFormatter>().Object,
            customerService.Object, currencyService.Object, storeService.Object, languageService.Object,
            new Mock<IPricingService>().Object, new Mock<ITaxService>().Object, validator.Object,
            translationService.Object, new Mock<IEnumTranslationService>().Object,
            contextAccessor.Object, _mediator.Object);
    }

    [TestMethod]
    public async Task AddProductToOrderDetails_KeepsInclAndExclTaxUnitPrices()
    {
        InsertOrderItemCommand sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<InsertOrderItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((c, _) => sent = (InsertOrderItemCommand)c)
            .ReturnsAsync(true);

        var warnings = await _service.AddProductToOrderDetails(_order, _product,
            new AddProductToOrderModel("o1", "p1", UnitPriceInclTax: 123, UnitPriceExclTax: 100, Quantity: 2,
                TaxRate: 23));

        Assert.AreEqual(0, warnings.Count);
        Assert.IsNotNull(sent);
        Assert.AreEqual(123, sent.OrderItem.UnitPriceInclTax);
        Assert.AreEqual(100, sent.OrderItem.UnitPriceExclTax);
        Assert.AreEqual(246, sent.OrderItem.PriceInclTax);
        Assert.AreEqual(200, sent.OrderItem.PriceExclTax);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task AddProductToOrderDetails_QuantityNotPositive_ReturnsWarning_NothingAdded(int quantity)
    {
        var warnings = await _service.AddProductToOrderDetails(_order, _product,
            new AddProductToOrderModel("o1", "p1", 10, 10, quantity, 0));

        CollectionAssert.AreEqual(new[] { "ShoppingCart.QuantityShouldPositive" }, warnings.ToArray());
        _mediator.Verify(m => m.Send(It.IsAny<InsertOrderItemCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
