using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class PaymentTransactionControllerRoutingTests
{
    [TestMethod]
    public void StorePaymentTransactionController_InheritsBasePaymentTransactionController() =>
        Assert.IsTrue(typeof(BasePaymentTransactionController).IsAssignableFrom(typeof(PaymentTransactionController)));

    [TestMethod]
    public void StorePaymentTransactionController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(PaymentTransactionController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void StorePaymentTransactionController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = (AreaAttribute)Attribute.GetCustomAttribute(typeof(PaymentTransactionController), typeof(AreaAttribute), false);
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaStore, areaAttr.RouteValue);
    }

    [TestMethod]
    public void StorePaymentTransactionController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(PaymentTransactionController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");
}
