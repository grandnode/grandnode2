using Grand.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class StateChangingActionVerbTests
{
    //a GET can be triggered by a link or an image on another site; these change the customer's data
    [TestMethod]
    [DataRow(typeof(OrderController), nameof(OrderController.CancelOrder))]
    [DataRow(typeof(ShoppingCartController), nameof(ShoppingCartController.ClearCart))]
    public void Action_AcceptsPostOnly(Type controller, string action)
    {
        var method = controller.GetMethod(action, BindingFlags.Public | BindingFlags.Instance)!;
        var verbs = method.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods).ToList();

        CollectionAssert.AreEquivalent(new[] { "POST" }, verbs);
    }
}
