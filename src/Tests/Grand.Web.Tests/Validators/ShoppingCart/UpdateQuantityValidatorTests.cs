using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Models.ShoppingCart;
using Grand.Web.Validators.ShoppingCart;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.ShoppingCart;

[TestClass]
public class UpdateQuantityValidatorTests
{
    private static UpdateQuantityValidator Validator(ShoppingCartItem item, bool cartEnabled, bool wishlistEnabled)
    {
        var permissionService = new Mock<IRequestPermissionService>();
        permissionService.Setup(p => p.Authorize(It.Is<Permission>(x =>
                x.SystemName == StandardPermission.EnableShoppingCart.SystemName)))
            .ReturnsAsync(cartEnabled);
        permissionService.Setup(p => p.Authorize(It.Is<Permission>(x =>
                x.SystemName == StandardPermission.EnableWishlist.SystemName)))
            .ReturnsAsync(wishlistEnabled);

        var shoppingCartService = new Mock<IShoppingCartService>();
        shoppingCartService.Setup(s => s.GetShoppingCart(It.IsAny<Customer>(), It.IsAny<string>(),
                It.IsAny<ShoppingCartType[]>()))
            .ReturnsAsync([item]);

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer());
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        return new UpdateQuantityValidator(new List<IValidatorConsumer<UpdateQuantityModel>>(),
            permissionService.Object, new ShoppingCartSettings(), shoppingCartService.Object, context.Object);
    }

    [TestMethod]
    public async Task CartItemDeclaredAsWishlist_WithoutCartPermission_IsRejected()
    {
        var item = new ShoppingCartItem { Id = "i1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart };

        var result = await Validator(item, false, true).ValidateAsync(new UpdateQuantityModel {
            ShoppingCartId = "i1", Quantity = 5, ShoppingCartType = ShoppingCartType.Wishlist
        });

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task OnHoldItemDeclaredAsCart_WithCartPermission_IsValid()
    {
        var item = new ShoppingCartItem { Id = "i1", ShoppingCartTypeId = ShoppingCartType.OnHoldCart };

        var result = await Validator(item, true, false).ValidateAsync(new UpdateQuantityModel {
            ShoppingCartId = "i1", Quantity = 5, ShoppingCartType = ShoppingCartType.ShoppingCart
        });

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task WishlistItem_WithoutWishlistPermission_IsRejected()
    {
        var item = new ShoppingCartItem { Id = "i1", ShoppingCartTypeId = ShoppingCartType.Wishlist };

        var result = await Validator(item, true, false).ValidateAsync(new UpdateQuantityModel {
            ShoppingCartId = "i1", Quantity = 5, ShoppingCartType = ShoppingCartType.Wishlist
        });

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task UnknownItem_IsRejected()
    {
        var item = new ShoppingCartItem { Id = "i1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart };

        var result = await Validator(item, true, true).ValidateAsync(new UpdateQuantityModel {
            ShoppingCartId = "other", Quantity = 5, ShoppingCartType = ShoppingCartType.ShoppingCart
        });

        Assert.IsFalse(result.IsValid);
    }
}
