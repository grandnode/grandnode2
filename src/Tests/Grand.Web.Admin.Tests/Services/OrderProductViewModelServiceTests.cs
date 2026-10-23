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
    [TestMethod]
    public async Task AddProductToOrderDetails_KeepsInclAndExclTaxUnitPrices()
    {
        var order = new Order { Id = "o1", CustomerId = "c1", StoreId = "s1", CustomerCurrencyCode = "USD" };
        var product = new Product { Id = "p1" };
        var orderService = new Mock<IOrderService>();
        orderService.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(product);
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(new Customer());
        var storeService = new Mock<IStoreService>();
        storeService.Setup(s => s.GetStoreById("s1")).ReturnsAsync(new Store());
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(s => s.GetCurrencyByCode("USD")).ReturnsAsync(new Currency());
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(s => s.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language());
        var validator = new Mock<IShoppingCartValidator>();
        validator.Setup(v => v.GetShoppingCartItemAttributeWarnings(It.IsAny<Customer>(), product,
            It.IsAny<ShoppingCartItem>(), It.IsAny<Language>(), It.IsAny<bool>())).ReturnsAsync(new List<string>());
        validator.Setup(v => v.GetShoppingCartItemGiftVoucherWarnings(It.IsAny<Customer>(), product,
            It.IsAny<ShoppingCartItem>(), It.IsAny<Language>())).ReturnsAsync(new List<string>());
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(new Mock<IWorkContext>().Object);
        var mediator = new Mock<IMediator>();
        InsertOrderItemCommand sent = null;
        mediator.Setup(m => m.Send(It.IsAny<InsertOrderItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((c, _) => sent = (InsertOrderItemCommand)c)
            .ReturnsAsync(true);
        var service = new OrderProductViewModelService(orderService.Object, productService.Object,
            new Mock<IProductAttributeService>().Object, new Mock<IProductAttributeFormatter>().Object,
            customerService.Object, currencyService.Object, storeService.Object, languageService.Object,
            new Mock<IPricingService>().Object, new Mock<ITaxService>().Object, validator.Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IEnumTranslationService>().Object,
            contextAccessor.Object, mediator.Object);

        var warnings = await service.AddProductToOrderDetails(
            new AddProductToOrderModel("o1", "p1", UnitPriceInclTax: 123, UnitPriceExclTax: 100, Quantity: 2,
                TaxRate: 23));

        Assert.AreEqual(0, warnings.Count);
        Assert.IsNotNull(sent);
        Assert.AreEqual(123, sent.OrderItem.UnitPriceInclTax);
        Assert.AreEqual(100, sent.OrderItem.UnitPriceExclTax);
        Assert.AreEqual(246, sent.OrderItem.PriceInclTax);
        Assert.AreEqual(200, sent.OrderItem.PriceExclTax);
    }
}
