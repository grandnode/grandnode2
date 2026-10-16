using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetShoppingCartItemsSubTotalQueryHandlerTests
{
    [TestMethod]
    public async Task Handle_PricesItemsForRequestCustomerInPrimaryCurrency_SkipsMissingProducts()
    {
        var product = new Product { Id = "p1" };
        var customer = new Customer { Id = "c1" };
        var store = new Store { Id = "s1" };
        var primaryCurrency = new Currency { Id = "usd" };
        var productService = new Mock<IProductService>();
        productService.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(c => c.GetPrimaryStoreCurrency()).ReturnsAsync(primaryCurrency);
        var pricingService = new Mock<IPricingService>();
        pricingService.Setup(p => p.GetUnitPrice(product, customer, store, primaryCurrency,
                ShoppingCartType.ShoppingCart, It.IsAny<int>(), It.IsAny<IList<CustomAttribute>>(),
                It.IsAny<double?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), false))
            .ReturnsAsync((20d, 0d, new List<ApplyDiscount>()));
        var handler = new GetShoppingCartItemsSubTotalQueryHandler(pricingService.Object, productService.Object,
            currencyService.Object);

        var subTotal = await handler.Handle(new GetShoppingCartItemsSubTotalQuery {
            Customer = customer,
            Store = store,
            Items = [
                new ShoppingCartItem { ProductId = "p1", Quantity = 2, ShoppingCartTypeId = ShoppingCartType.ShoppingCart },
                new ShoppingCartItem { ProductId = "p1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart },
                new ShoppingCartItem { ProductId = "missing", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart }
            ]
        }, CancellationToken.None);

        Assert.AreEqual(60, subTotal);
        pricingService.Verify(p => p.GetSubTotal(It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<bool>()),
            Times.Never);
    }
}
