using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class RecurringPaymentControllerRoutingTests
{
    [TestMethod]
    public void StoreRecurringPaymentController_InheritsBaseRecurringPaymentController() =>
        Assert.IsTrue(typeof(BaseRecurringPaymentController).IsAssignableFrom(typeof(RecurringPaymentController)));

    [TestMethod]
    public void StoreRecurringPaymentController_HasAutoValidateAntiforgeryToken() =>
        Assert.IsTrue(typeof(RecurringPaymentController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute), false)
            .Length > 0);

    [TestMethod]
    public void StoreRecurringPaymentController_HasAreaAttributeWithStoreArea()
    {
        var areaAttr = Assert.IsInstanceOfType<AreaAttribute>(Attribute.GetCustomAttribute(typeof(RecurringPaymentController), typeof(AreaAttribute), false));
        Assert.IsNotNull(areaAttr, "Missing [Area].");
        Assert.AreEqual(Constants.AreaStore, areaAttr.RouteValue);
    }

    [TestMethod]
    public void StoreRecurringPaymentController_HasAuthorizeStoreAttribute() =>
        Assert.IsTrue(typeof(RecurringPaymentController).IsDefined(typeof(AuthorizeStoreAttribute), false),
            "Missing [AuthorizeStore].");
}
