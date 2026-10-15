using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetShoppingCartItemsSubTotalQueryHandlerTests
{
    [TestMethod]
    public async Task Handle_SumsSubtotalsWithoutDiscounts_SkipsMissingProducts()
    {
        var product = new Product { Id = "p1" };
        var productService = new Mock<IProductService>();
        productService.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var pricingService = new Mock<IPricingService>();
        pricingService.Setup(p => p.GetSubTotal(It.IsAny<ShoppingCartItem>(), product, false))
            .ReturnsAsync((40d, 0d, new List<ApplyDiscount>()));
        var handler = new GetShoppingCartItemsSubTotalQueryHandler(pricingService.Object, productService.Object);

        var subTotal = await handler.Handle(new GetShoppingCartItemsSubTotalQuery {
            Items = [
                new ShoppingCartItem { ProductId = "p1" },
                new ShoppingCartItem { ProductId = "p1" },
                new ShoppingCartItem { ProductId = "missing" }
            ]
        }, CancellationToken.None);

        Assert.AreEqual(80, subTotal);
        pricingService.Verify(p => p.GetSubTotal(It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), true), Times.Never);
    }
}
