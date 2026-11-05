using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shipping.ByWeight;
using Shipping.ByWeight.Services;

namespace Grand.Modules.Tests.Plugins.Shipping;

[TestClass]
public class ByWeightShippingCalcPluginTests
{
    [TestMethod]
    public async Task GetShippingOptions_CustomerWithoutCheckoutAttributes_DoesNotThrow()
    {
        var product = new Product { Weight = 2 };
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductById(product.Id, It.IsAny<bool>())).ReturnsAsync(product);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext.CurrentStore).Returns(new Grand.Domain.Stores.Store());
        contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(new Customer());
        var shippingMethodService = new Mock<IShippingMethodService>();
        shippingMethodService
            .Setup(x => x.GetAllShippingMethods(It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<string>()))
            .ReturnsAsync(new List<ShippingMethod>());
        var provider = new ByWeightShippingCalcPlugin(shippingMethodService.Object, contextAccessor.Object,
            Mock.Of<IRequestTranslationService>(), productService.Object, Mock.Of<ICheckoutAttributeParser>(),
            Mock.Of<ICurrencyService>(), Mock.Of<IPricingService>(), Mock.Of<IShippingByWeightService>(),
            new ByWeightShippingSettings());
        //a customer who never chose checkout attributes, or whose choice the last order cleared
        var request = new GetShippingOptionRequest {
            Customer = new Customer(),
            StoreId = "store",
            ShippingAddress = new Address(),
            Items = [
                new GetShippingOptionRequest.PackageItem(new ShoppingCartItem
                    { ProductId = product.Id, Quantity = 1, IsFreeShipping = true })
            ]
        };

        var response = await provider.GetShippingOptions(request);

        Assert.IsTrue(response.Success);
    }
}
