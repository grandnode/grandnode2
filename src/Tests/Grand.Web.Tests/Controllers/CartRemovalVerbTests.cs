using Grand.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class CartRemovalVerbTests
{
    //a GET can be triggered by a link or an image on another site; these change the customer's cart or wishlist
    [TestMethod]
    [DataRow(typeof(ShoppingCartController), nameof(ShoppingCartController.RemoveDiscountCoupon))]
    [DataRow(typeof(ShoppingCartController), nameof(ShoppingCartController.RemoveGiftVoucherCode))]
    [DataRow(typeof(WishlistController), nameof(WishlistController.DeleteItemFromWishlist))]
    public void Action_AcceptsPostOnly(Type controller, string action)
    {
        var method = controller.GetMethod(action, BindingFlags.Public | BindingFlags.Instance)!;
        var verbs = method.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods).ToList();

        CollectionAssert.AreEquivalent(new[] { "POST" }, verbs);
    }
}
