using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerShoppingCartViewModelServiceTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IShoppingCartService> _shoppingCartServiceMock;
    private CustomerShoppingCartViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _storeServiceMock = new Mock<IStoreService>();
        _productServiceMock = new Mock<IProductService>();
        _shoppingCartServiceMock = new Mock<IShoppingCartService>();

        _service = new CustomerShoppingCartViewModelService(
            _customerServiceMock.Object,
            _storeServiceMock.Object,
            _productServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IContextAccessor>().Object,
            new Mock<ITaxService>().Object,
            new Mock<IPricingService>().Object,
            new Mock<IPriceFormatter>().Object,
            new Mock<IProductAttributeFormatter>().Object,
            _shoppingCartServiceMock.Object);
    }

    [TestMethod]
    public async Task PrepareShoppingCartItemModel_NoItemsOfTheRequestedType_ReturnsEmpty_WithoutLookups()
    {
        var customer = new Customer { Id = "c1" };
        customer.ShoppingCartItems.Add(new ShoppingCartItem {
            Id = "w1", ShoppingCartTypeId = ShoppingCartType.Wishlist, ProductId = "p1"
        });

        var items = await _service.PrepareShoppingCartItemModel(customer, (int)ShoppingCartType.ShoppingCart);

        Assert.AreEqual(0, items.Count);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _customerServiceMock.Verify(c => c.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareShoppingCartItemModel_SkipsItemsWhoseProductNoLongerExists()
    {
        var customer = new Customer { Id = "c1" };
        customer.ShoppingCartItems.Add(new ShoppingCartItem {
            Id = "sci1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart, ProductId = "gone", StoreId = "s1"
        });
        _storeServiceMock.Setup(s => s.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1" });
        _productServiceMock.Setup(p => p.GetProductById("gone", false)).ReturnsAsync((Product)null);

        var items = await _service.PrepareShoppingCartItemModel(customer, (int)ShoppingCartType.ShoppingCart);

        Assert.AreEqual(0, items.Count);
        _productServiceMock.Verify(p => p.GetProductById("gone", false), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCart_KnownItem_DeletesItAndSavesTheCustomer()
    {
        var customer = new Customer { Id = "c1" };
        var item = new ShoppingCartItem { Id = "sci1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart };
        customer.ShoppingCartItems.Add(item);

        await _service.DeleteCart(customer, "sci1");

        _shoppingCartServiceMock.Verify(s => s.DeleteShoppingCartItem(customer, item, true, true), Times.Once);
        _customerServiceMock.Verify(c => c.UpdateCustomerInAdminPanel(customer), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCart_UnknownItem_DoesNothing()
    {
        var customer = new Customer { Id = "c1" };

        await _service.DeleteCart(customer, "missing");

        _shoppingCartServiceMock.VerifyNoOtherCalls();
        _customerServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task UpdateCart_UnknownItem_ReturnsNoWarnings_WithoutUpdating()
    {
        var customer = new Customer { Id = "c1" };

        var warnings = await _service.UpdateCart(customer, "missing", 9.99);

        Assert.AreEqual(0, warnings.Count);
        _shoppingCartServiceMock.VerifyNoOtherCalls();
    }
}
